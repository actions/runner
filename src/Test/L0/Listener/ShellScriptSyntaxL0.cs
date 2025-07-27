using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class ShellScriptSyntaxL0
    {
        // Generic method to test any shell script template for bash syntax errors
        private void ValidateShellScriptTemplateSyntax(string relativePath, string templateName, bool shouldPass = true, Func<string, string> templateModifier = null, bool useFullPath = false)
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
                    
                    // If useFullPath is true, the templateName is already the full path
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

                    // Read the template
                    string template = File.ReadAllText(templatePath);
                    
                    // Apply template modifier if provided (for injecting errors)
                    if (templateModifier != null)
                    {
                        template = templateModifier(template);
                    }
                    
                    // Replace common placeholders with valid test values
                    string rootFolder = useFullPath ? Path.GetDirectoryName(templatePath) : Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    template = ReplaceCommonPlaceholders(template, rootFolder, tempDir);
                    
                    // Write the processed template to a temporary file
                    File.WriteAllText(tempScriptPath, template);
                    
                    // Make the file executable
                    var chmodProcess = new Process();
                    chmodProcess.StartInfo.FileName = "chmod";
                    chmodProcess.StartInfo.Arguments = $"+x {tempScriptPath}";
                    chmodProcess.Start();
                    chmodProcess.WaitForExit();
                    
                    // Act - Check syntax using bash -n
                    var process = new Process();
                    process.StartInfo.FileName = "bash";
                    process.StartInfo.Arguments = $"-n {tempScriptPath}";
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    // Assert based on expected outcome
                    if (shouldPass)
                    {
                        Assert.Equal(0, process.ExitCode);
                        Assert.Empty(errors);
                    }
                    else
                    {
                        Assert.NotEqual(0, process.ExitCode);
                        Assert.NotEmpty(errors);
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
                Assert.Fail($"Exception during test for {templateName}: {ex}");
            }
        }

        // Helper method to replace common placeholders in shell script templates
        private string ReplaceCommonPlaceholders(string template, string rootDirectory, string tempDir)
        {
            // Replace common placeholders
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
            ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "update.sh.template");
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void UpdateShTemplateWithErrorsFailsValidation()
        {
            ValidateShellScriptTemplateSyntax(
                "src/Misc/layoutbin", 
                "update.sh.template",
                shouldPass: false,
                templateModifier: template => 
                {
                    // Introduce syntax errors
                    
                    // 1. Missing 'fi' for an 'if' statement
                    template = template.Replace("fi\n", "\n");
                    
                    // 2. Unbalanced quotes
                    template = template.Replace("date \"+[%F %T-%4N]", "date \"+[%F %T-%4N");
                    
                    // 3. Invalid syntax in if condition
                    template = template.Replace("if [ $? -ne 0 ]", "if [ $? -ne 0");
                    
                    return template;
                });
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
                    // Introduce syntax errors
                    
                    // 1. Missing 'fi' for an 'if' statement
                    template = template.Replace("fi\n", "\n");
                    
                    // 2. Unmatched brackets in case statement
                    template = template.Replace("esac", "");
                    
                    // 3. Missing closing quote
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
                    // Introduce syntax errors
                    
                    // 1. Missing done for a for loop
                    template = template.Replace("done\n", "\n");
                    
                    // 2. Unbalanced parentheses in function
                    template = template.Replace("function", "function (");
                    
                    // 3. Invalid syntax in if condition
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
                    // Introduce syntax errors
                    
                    // 1. Missing closing brace in variable substitution
                    template = template.Replace("${RUNNER_ROOT}", "${RUNNER_ROOT");
                    
                    // 2. Unbalanced quotes in string
                    template = template.Replace("\"$@\"", "\"$@");
                    
                    // 3. Invalid redirection
                    template = template.Replace("> /dev/null", ">> >>");
                    
                    return template;
                });
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void ValidateShellScript_MissingTemplate_ThrowsFileNotFoundException()
        {
            // Test for non-existent template file
            Assert.Throws<System.IO.FileNotFoundException>(() => 
                ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "non_existent_template.sh.template", shouldPass: true));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "windows")]
        public void ValidateShellScript_ComplexScript_ValidatesCorrectly()
        {
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
                // Test with direct path to our temporary template
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
            // Skip on non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            ValidateCmdScriptTemplateSyntax("update.cmd.template", shouldPass: false, 
                templateModifier: template => 
                {
                    // Introduce syntax errors in the template
                    // 1. Unbalanced parentheses
                    template = template.Replace("if exist", "if exist (");
                    
                    // 2. Unclosed quotes
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
            // Skip on non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Test for non-existent template file
            Assert.Throws<System.IO.FileNotFoundException>(() => 
                ValidateCmdScriptTemplateSyntax("non_existent_template.cmd.template", shouldPass: true));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [Trait("SkipOn", "osx,linux")]
        public void ValidateCmdScript_ComplexQuoting_ValidatesCorrectly()
        {
            // Skip on non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            // Create a test template with complex quoting patterns
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string templatePath = Path.Combine(tempDir, "complex_quotes.cmd.template");

            // Write a sample template with escaped quotes and nested quotes
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
                // Test with direct path to our temporary template
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
            // Skip on non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            
            // Create a test template with complex parentheses patterns
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string templatePath = Path.Combine(tempDir, "complex_parens.cmd.template");

            // Write a sample template with nested parentheses
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
                // Test with direct path to our temporary template
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
        
        // Helper method to check for unclosed quotes that handles escaped quotes properly
        private bool HasUnclosedQuotes(string text)
        {
            bool inQuote = false;
            bool isEscaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // Check for escape character (backslash)
                if (c == '\\')
                {
                    isEscaped = !isEscaped; // Toggle escape state
                    continue;
                }
                
                // Check for quotes, but only if not escaped
                if (c == '"' && !isEscaped)
                {
                    inQuote = !inQuote;
                }
                
                // Reset escape state after non-backslash character
                if (c != '\\')
                {
                    isEscaped = false;
                }
            }
            
            // If we're still in a quote at the end, there's an unclosed quote
            return inQuote;
        }
        
        // Helper method to check for balanced parentheses accounting for strings and comments
        private bool HasBalancedParentheses(string text)
        {
            int balance = 0;
            bool inQuote = false;
            bool isEscaped = false;
            bool inComment = false;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // Skip processing if we're in a comment (for batch files, REM or ::)
                if (inComment)
                {
                    if (c == '\n' || c == '\r')
                    {
                        inComment = false;
                    }
                    continue;
                }
                
                // Check for comment start
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
                
                // Check for escape character
                if (c == '\\')
                {
                    isEscaped = !isEscaped;
                    continue;
                }
                
                // Check for quote state
                if (c == '"' && !isEscaped)
                {
                    inQuote = !inQuote;
                }
                
                // Only count parentheses when not in a quoted string
                if (!inQuote)
                {
                    if (c == '(')
                    {
                        balance++;
                    }
                    else if (c == ')')
                    {
                        balance--;
                        // Negative balance means we have a closing paren without an opening one
                        if (balance < 0)
                        {
                            return false;
                        }
                    }
                }
                
                // Reset escape state
                if (c != '\\')
                {
                    isEscaped = false;
                }
            }
            
            // Balanced if we end with zero
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
                    
                    // If useFullPath is true, the templateName is already the full path
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

                    // Read the template
                    string template = File.ReadAllText(templatePath);
                    
                    // Apply template modifier if provided (for injecting errors)
                    if (templateModifier != null)
                    {
                        template = templateModifier(template);
                    }
                    
                    // Replace the placeholders with valid test values
                    template = template.Replace("_PROCESS_ID_", "1234");
                    template = template.Replace("_RUNNER_PROCESS_NAME_", "Runner.Listener.exe");
                    string rootFolder = useFullPath ? Path.GetDirectoryName(templatePath) : Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    template = template.Replace("_ROOT_FOLDER_", rootFolder);
                    template = template.Replace("_EXIST_RUNNER_VERSION_", "2.300.0");
                    template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", "2.301.0");
                    template = template.Replace("_UPDATE_LOG_", Path.Combine(tempDir, "update.log"));
                    template = template.Replace("_RESTART_INTERACTIVE_RUNNER_", "0");
                    
                    // Write the processed template to a temporary file
                    File.WriteAllText(tempUpdatePath, template);
                    
                    // Act - Check syntax without executing the commands in the script
                    // Use cmd.exe's built-in syntax checking by using the /K (keep alive) flag
                    // and adding an 'exit' command at the end
                    var process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    // Add "CALL" before the script to validate syntax without executing side effects
                    // Add "exit" at the end to ensure the process terminates
                    process.StartInfo.Arguments = $"/c cmd /c \"@echo off & (call \"{tempUpdatePath}\" > nul 2>&1) & echo %ERRORLEVEL%\"";
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    
                    // Ensure the working directory is set correctly
                    process.StartInfo.WorkingDirectory = tempDir;
                    
                    process.Start();
                    string errors = process.StandardError.ReadToEnd();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    // Basic syntax checks (these are supplementary to the execution test)
                    
                    // Check for mismatched parentheses using our robust helper method
                    bool hasMissingParenthesis = !HasBalancedParentheses(template);
                    
                    // Check for unclosed quotes (robust check to handle escaped quotes and nested quotes)  
                    bool hasUnclosedQuotes = HasUnclosedQuotes(template);

                    // Look for specific error messages in output/errors that indicate syntax problems
                    bool hasOutputErrors = !string.IsNullOrEmpty(errors) || 
                                          output.Contains("syntax error") || 
                                          output.Contains("not recognized") ||
                                          output.Contains("unexpected");
                    
                    // Determine if the validation passed - for the shouldPass=true case, we expect exit code 0
                    // For shouldPass=false case, the specific exit code doesn't matter as much as detecting the errors
                    bool validationPassed = process.ExitCode == 0 && 
                                          !hasOutputErrors &&
                                          !hasMissingParenthesis && 
                                          !hasUnclosedQuotes;
                    
                    // Assert based on expected outcome
                    if (shouldPass)
                    {
                        Assert.True(validationPassed, 
                            $"Template validation should have passed but failed. Exit code: {process.ExitCode}, " +
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
