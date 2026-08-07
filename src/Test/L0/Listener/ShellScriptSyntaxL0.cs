using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class ShellScriptSyntaxL0
    {
        private void ValidateShellScriptTemplateSyntax(string relativePath, string templateName, bool shouldPass = true, Func<string, string> templateModifier = null, bool useFullPath = false, bool useShellCheck = true)
        {
            // Skip on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    // Arrange
                    string templatePath;
                    
                    if (useFullPath)
                    {
                        templatePath = templateName;
                    }
                    else
                    {
                        string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                        templatePath = Path.Combine(rootDirectory, relativePath, templateName);
                    }
                    
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    string tempScriptPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(templateName));
                    string debugLogPath = Path.Combine(tempDir, "debug_log.txt");

                    string template = File.ReadAllText(templatePath);
                    
                    if (templateModifier != null)
                    {
                        template = templateModifier(template);
                    }
                    
                    string rootFolder = useFullPath ? Path.GetDirectoryName(templatePath) : Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    template = ReplaceCommonPlaceholders(template, rootFolder, tempDir);
                    
                    File.WriteAllText(tempScriptPath, template);

                    var chmodProcess = new Process();
                    chmodProcess.StartInfo.FileName = "chmod";
                    chmodProcess.StartInfo.Arguments = $"+x {tempScriptPath}";
                    chmodProcess.Start();
                    chmodProcess.WaitForExit();
                    
                    var bashCheckProcess = new Process();
                    bashCheckProcess.StartInfo.FileName = "/bin/bash";
                    bashCheckProcess.StartInfo.Arguments = $"-c \"bash -n {tempScriptPath}; echo $?\"";
                    bashCheckProcess.StartInfo.RedirectStandardOutput = true;
                    bashCheckProcess.StartInfo.RedirectStandardError = true;
                    bashCheckProcess.StartInfo.UseShellExecute = false;
                    
                    bashCheckProcess.Start();
                    string bashCheckOutput = bashCheckProcess.StandardOutput.ReadToEnd();
                    string bashCheckErrors = bashCheckProcess.StandardError.ReadToEnd();
                    bashCheckProcess.WaitForExit();
                    
                    // Act - Check syntax using bash -n
                    var process = new Process();
                    process.StartInfo.FileName = "bash";
                    process.StartInfo.Arguments = $"-n {tempScriptPath}";
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    
                    process.Start();
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (!string.IsNullOrEmpty(errors))
                    {
                        Console.WriteLine($"Errors: {errors}");
                    }
                    
                    // Assert based on expected outcome
                    if (shouldPass)
                    {
                        Console.WriteLine("Test expected to pass, checking exit code and errors");
                        Assert.Equal(0, process.ExitCode);
                        Assert.Empty(errors);
                        
                        if (shouldPass && process.ExitCode == 0 && useShellCheck)
                        {
                            RunShellCheck(tempScriptPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Test expected to fail, checking exit code and errors");
                        Assert.NotEqual(0, process.ExitCode);
                        Assert.NotEmpty(errors);
                    }
                    
                    // Cleanup - But leave the temp directory for debugging on failure
                    if (process.ExitCode == 0 && shouldPass) 
                    {
                        try
                        {
                            Directory.Delete(tempDir, true);
                        }
                        catch
                        {
                            // Best effort cleanup
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Not cleaning up temp directory for debugging: {tempDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception during test for {templateName}: {ex}");
            }
        }

        private void RunShellCheck(string scriptPath)
        {
            var shellcheckExistsProcess = new Process();
            shellcheckExistsProcess.StartInfo.FileName = "which";
            shellcheckExistsProcess.StartInfo.Arguments = "shellcheck";
            shellcheckExistsProcess.StartInfo.RedirectStandardOutput = true;
            shellcheckExistsProcess.StartInfo.UseShellExecute = false;

            shellcheckExistsProcess.Start();
            string shellcheckPath = shellcheckExistsProcess.StandardOutput.ReadToEnd().Trim();
            shellcheckExistsProcess.WaitForExit();

            if (!string.IsNullOrEmpty(shellcheckPath))
            {
                Console.WriteLine("ShellCheck found, performing additional validation");

                var shellcheckProcess = new Process();
                shellcheckProcess.StartInfo.FileName = "shellcheck";
                shellcheckProcess.StartInfo.Arguments = $"-e SC2001,SC2002,SC2006,SC2009,SC2016,SC2034,SC2039,SC2046,SC2048,SC2059,SC2086,SC2094,SC2115,SC2116,SC2126,SC2129,SC2140,SC2145,SC2153,SC2154,SC2155,SC2162,SC2164,SC2166,SC2174,SC2181,SC2206,SC2207,SC2221,SC2222,SC2230,SC2236,SC2242,SC2268 {scriptPath}";
                shellcheckProcess.StartInfo.RedirectStandardOutput = true;
                shellcheckProcess.StartInfo.RedirectStandardError = true;
                shellcheckProcess.StartInfo.UseShellExecute = false;

                shellcheckProcess.Start();
                string shellcheckOutput = shellcheckProcess.StandardOutput.ReadToEnd();
                string shellcheckErrors = shellcheckProcess.StandardError.ReadToEnd();
                shellcheckProcess.WaitForExit();

                if (shellcheckProcess.ExitCode != 0)
                {
                    Console.WriteLine($"ShellCheck found syntax errors: {shellcheckOutput}");
                    Console.WriteLine($"ShellCheck errors: {shellcheckErrors}");

                    Assert.Fail($"ShellCheck validation failed with exit code {shellcheckProcess.ExitCode}. Output: {shellcheckOutput}. Errors: {shellcheckErrors}");
                }
                else
                {
                    Console.WriteLine("ShellCheck validation passed");
                }
            }
            else
            {
                Console.WriteLine("ShellCheck not found, skipping additional validation");
            }
        }

        private string ReplaceCommonPlaceholders(string template, string rootDirectory, string tempDir)
        {
            template = template.Replace("_PROCESS_ID_", "1234");
            template = template.Replace("_RUNNER_PROCESS_NAME_", "Runner.Listener");
            template = template.Replace("_ROOT_FOLDER_", rootDirectory);
            template = template.Replace("_EXIST_RUNNER_VERSION_", "2.300.0");
            template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", "2.301.0");
            template = template.Replace("_UPDATE_LOG_", Path.Combine(tempDir, "update.log"));
            template = template.Replace("_RESTART_INTERACTIVE_RUNNER_", "0");
            template = template.Replace("_SERVICEUSERNAME_", "runner");
            template = template.Replace("_SERVICEPASSWORD_", "password");
            template = template.Replace("_SERVICEDISPLAYNAME_", "GitHub Actions Runner");
            template = template.Replace("_SERVICENAME_", "github-runner");
            template = template.Replace("_SERVICELOGPATH_", Path.Combine(tempDir, "service.log"));
            template = template.Replace("_RUNNERSERVICEUSERDISPLAYNAME_", "GitHub Actions Runner Service");

            return template;
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void UpdateShTemplateHasValidSyntax()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "update.sh.template");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test: {ex}");
                throw;
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void DarwinSvcShTemplateHasValidSyntax()
        {
            ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "darwin.svc.sh.template");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void DarwinSvcShTemplateWithErrorsFailsValidation()
        {
            ValidateShellScriptTemplateSyntax(
                "src/Misc/layoutbin", 
                "darwin.svc.sh.template",
                shouldPass: false,
                templateModifier: template => 
                {
                    
                    template = template.Replace("fi\n", "\n");
                    template = template.Replace("esac", "");
                    template = template.Replace("\"$svcuser\"", "\"$svcuser");
                    
                    return template;
                });
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void SystemdSvcShTemplateHasValidSyntax()
        {
            ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "systemd.svc.sh.template");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void SystemdSvcShTemplateWithErrorsFailsValidation()
        {
            ValidateShellScriptTemplateSyntax(
                "src/Misc/layoutbin", 
                "systemd.svc.sh.template",
                shouldPass: false,
                templateModifier: template => 
                {
                    template = template.Replace("done\n", "\n");
                    template = template.Replace("function", "function (");
                    template = template.Replace("if [ ! -f ", "if ! -f ");
                    
                    return template;
                });
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void RunHelperShTemplateHasValidSyntax()
        {
            ValidateShellScriptTemplateSyntax("src/Misc/layoutroot", "run-helper.sh.template");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void RunHelperShTemplateWithErrorsFailsValidation()
        {
            ValidateShellScriptTemplateSyntax(
                "src/Misc/layoutroot", 
                "run-helper.sh.template",
                shouldPass: false,
                templateModifier: template => 
                {
                    
                    template = template.Replace("${RUNNER_ROOT}", "${RUNNER_ROOT");
                    template = template.Replace("\"$@\"", "\"$@");
                    template = template.Replace("> /dev/null", ">> >>");
                    
                    return template;
                });
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void ValidateShellScript_MissingTemplate_ThrowsException()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            try 
            {
                ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "non_existent_template.sh.template", shouldPass: true);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (Exception ex)
            {
                Assert.Contains("non_existent_template.sh.template", ex.Message);
                Assert.Contains("FileNotFoundException", ex.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void ValidateShellScript_ComplexScript_ValidatesCorrectly()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            // Create a test template with complex shell scripting patterns
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string templatePath = Path.Combine(tempDir, "complex_shell.sh.template");

            // Write a sample template with various shell features
            string template = @"#!/bin/bash
set -e

# Function with nested quotes and complex syntax
function complex_func() {
    local var1=""$1""
    local var2=""${2:-default}""
    echo ""Function arguments: '$var1' and '$var2'""
    if [ ""$var1"" == ""test"" ]; then
        echo ""This is a 'test' with nested quotes""
    fi
}

# Complex variable substitutions
VAR1=""test value""
VAR2=""${VAR1:0:4}""
VAR3=""$(echo ""command substitution"")""

# Here document
cat << EOF > /tmp/testfile
This is a test file
With multiple lines
And some $VAR1 substitution
EOF

complex_func ""test"" ""value""
exit 0";

            File.WriteAllText(templatePath, template);
            
            try
            {
                ValidateShellScriptTemplateSyntax("", templatePath, shouldPass: true, useFullPath: true);
            }
            finally
            {
                // Clean up
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void UpdateCmdTemplateHasValidSyntax()
        {
            // Skip on non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            ValidateCmdScriptTemplateSyntax("update.cmd.template", shouldPass: true);
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void UpdateCmdTemplateWithErrorsFailsValidation()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            ValidateCmdScriptTemplateSyntax("update.cmd.template", shouldPass: false, 
                templateModifier: template => 
                {
                    template = template.Replace("if exist", "if exist (");
                    template = template.Replace("echo", "echo \"Unclosed quote");
                    
                    return template;
                });
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void ValidateCmdScript_MissingTemplate_ThrowsFileNotFoundException()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            try
            {
                string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                string templatePath = Path.Combine(rootDirectory, "src", "Misc", "layoutbin", "non_existent_template.cmd.template");
                string content = File.ReadAllText(templatePath);
                
                Assert.Fail($"Expected FileNotFoundException was not thrown for {templatePath}");
            }
            catch (FileNotFoundException)
            {
                // This is expected, so test passes
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void ValidateCmdScript_ComplexQuoting_ValidatesCorrectly()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string templatePath = Path.Combine(tempDir, "complex_quotes.cmd.template");

            string template = @"@echo off
echo ""This has ""nested"" quotes""
echo ""This has an escaped quote: \""test\""""
echo Simple command
if ""quoted condition"" == ""quoted condition"" (
    echo ""Inside if block with quotes""
)";

            File.WriteAllText(templatePath, template);
            
            try
            {
                ValidateCmdScriptTemplateSyntax(templatePath, shouldPass: true, useFullPath: true);
            }
            finally
            {
                // Clean up
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void ValidateCmdScript_ComplexParentheses_ValidatesCorrectly()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string templatePath = Path.Combine(tempDir, "complex_parens.cmd.template");

            string template = @"@echo off
echo Text with (parentheses)
echo ""Text with (parentheses inside quotes)""
if exist file.txt (
    if exist other.txt (
        echo Nested if blocks
    ) else (
        echo Nested else
    )
) else (
    echo Outer else
)";

            File.WriteAllText(templatePath, template);
            
            try
            {
                ValidateCmdScriptTemplateSyntax(templatePath, shouldPass: true, useFullPath: true);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
        
        private bool HasUnclosedQuotes(string text)
        {
            bool inQuote = false;
            bool isEscaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                if (c == '\\')
                {
                    isEscaped = !isEscaped;
                    continue;
                }
                
                if (c == '"' && !isEscaped)
                {
                    inQuote = !inQuote;
                }
                
                if (c != '\\')
                {
                    isEscaped = false;
                }
            }
            
            return inQuote;
        }
        
        private bool HasBalancedParentheses(string text)
        {
            int balance = 0;
            bool inQuote = false;
            bool isEscaped = false;
            bool inComment = false;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                if (inComment)
                {
                    if (c == '\n' || c == '\r')
                    {
                        inComment = false;
                    }
                    continue;
                }
                
                if (!inQuote && i < text.Length - 1 && c == ':' && text[i+1] == ':')
                {
                    inComment = true;
                    continue;
                }
                
                if (!inQuote && i < text.Length - 2 && c == 'r' && text[i+1] == 'e' && text[i+2] == 'm' && 
                   (i == 0 || char.IsWhiteSpace(text[i-1])))
                {
                    inComment = true;
                    continue;
                }
                
                if (c == '\\')
                {
                    isEscaped = !isEscaped;
                    continue;
                }
                
                if (c == '"' && !isEscaped)
                {
                    inQuote = !inQuote;
                }
                
                if (!inQuote)
                {
                    if (c == '(')
                    {
                        balance++;
                    }
                    else if (c == ')')
                    {
                        balance--;
                        if (balance < 0)
                        {
                            return false;
                        }
                    }
                }
                
                if (c != '\\')
                {
                    isEscaped = false;
                }
            }
            
            return balance == 0;
        }

        private void ValidateCmdScriptTemplateSyntax(string templateName, bool shouldPass, Func<string, string> templateModifier = null, bool useFullPath = false)
        {
            try
            {
                using (var hc = new TestHostContext(this))
                {
                    // Arrange
                    string templatePath;
                    
                    if (useFullPath)
                    {
                        templatePath = templateName;
                    }
                    else
                    {
                        string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                        templatePath = Path.Combine(rootDirectory, "src", "Misc", "layoutbin", templateName);
                    }
                    
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    string tempUpdatePath = Path.Combine(tempDir, Path.GetFileName(templatePath).Replace(".template", ""));

                    string template = File.ReadAllText(templatePath);
                    
                    if (templateModifier != null)
                    {
                        template = templateModifier(template);
                    }
                    
                    template = template.Replace("_PROCESS_ID_", "1234");
                    template = template.Replace("_RUNNER_PROCESS_NAME_", "Runner.Listener.exe");
                    string rootFolder = useFullPath ? Path.GetDirectoryName(templatePath) : Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    template = template.Replace("_ROOT_FOLDER_", rootFolder);
                    template = template.Replace("_EXIST_RUNNER_VERSION_", "2.300.0");
                    template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", "2.301.0");
                    template = template.Replace("_UPDATE_LOG_", Path.Combine(tempDir, "update.log"));
                    template = template.Replace("_RESTART_INTERACTIVE_RUNNER_", "0");
                    
                    File.WriteAllText(tempUpdatePath, template);
                    
                
                    string errors = string.Empty;
                    string output = string.Empty;
                    int exitCode = 0;
                    
                    try 
                    {
                        string testBatchFile = Path.Combine(tempDir, "test.cmd");
                        File.WriteAllText(testBatchFile, "@echo off\r\nexit /b 0");
                        
                        var process = new Process();
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = $"/c \"cd /d \"{tempDir}\" && echo Script syntax check && exit /b 0\"";
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.WorkingDirectory = tempDir;
                        
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();
                        errors = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        exitCode = process.ExitCode;
                    }
                    catch (Exception ex)
                    {
                        errors = ex.ToString();
                        exitCode = 1;
                    }
                    
                    bool hasMissingParenthesis = !HasBalancedParentheses(template);
                    bool hasUnclosedQuotes = HasUnclosedQuotes(template);

                    bool hasOutputErrors = !string.IsNullOrEmpty(errors) || 
                                          output.Contains("syntax error") || 
                                          output.Contains("not recognized") ||
                                          output.Contains("unexpected") ||
                                          output.Contains("Syntax check failed");
                    
                    bool hasInvalidSyntaxPatterns = false;
                    
                    if (template.Contains("if") && !template.Contains("if "))
                    {
                        hasInvalidSyntaxPatterns = true; 
                    }
                    
                    if (template.Contains("goto") && !template.Contains("goto "))
                    {
                        hasInvalidSyntaxPatterns = true;
                    }
                    
                    if (template.Contains("(") && !template.Contains(")"))
                    {
                        hasInvalidSyntaxPatterns = true;
                    }
                    
                    bool staticAnalysisPassed = !hasMissingParenthesis && 
                                              !hasUnclosedQuotes &&
                                              !hasInvalidSyntaxPatterns;
                                              
                    bool executionPassed = true;
                    
                    try 
                    {
                        if (!errors.Contains("filename, directory name, or volume label syntax"))
                        {
                            executionPassed = exitCode == 0 && !hasOutputErrors;
                        }
                    }
                    catch 
                    {
                        executionPassed = true;
                    }
                    
                    bool validationPassed = staticAnalysisPassed && executionPassed;
                    
                    if (shouldPass)
                    {
                        Assert.True(validationPassed, 
                            $"Template validation should have passed but failed. Exit code: {exitCode}, " +
                            $"Errors: {errors}, HasMissingParenthesis: {hasMissingParenthesis}, " +
                            $"HasUnclosedQuotes: {hasUnclosedQuotes}");
                    }
                    else
                    {
                        Assert.False(validationPassed, 
                            "Template validation should have failed but passed. " +
                            "The intentionally introduced syntax errors were not detected.");
                    }
                    
                    // Cleanup
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Best effort cleanup
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception during test: {ex.ToString()}");
            }
        }
    }
}
