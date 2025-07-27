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
        private void ValidateShellScriptTemplateSyntax(string relativePath, string templateName, bool shouldPass = true, Func<string, string> templateModifier = null)
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
                    string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    string templatePath = Path.Combine(rootDirectory, relativePath, templateName);
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
                    template = ReplaceCommonPlaceholders(template, rootDirectory, tempDir);
                    
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
        public void SystemdSvcShTemplateHasValidSyntax()
        {
            ValidateShellScriptTemplateSyntax("src/Misc/layoutbin", "systemd.svc.sh.template");
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
        public void UpdateShTemplateHasCorrectVariableReferencesAndIfStructure()
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
                    string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    string templatePath = Path.Combine(rootDirectory, "src", "Misc", "layoutbin", "update.sh.template");
                    
                    // Read the template
                    string template = File.ReadAllText(templatePath);
                    
                    // Assert
                    // 1. Check that $restartinteractiverunner is correctly referenced with $ in if condition
                    Assert.Contains("if [[ \"$currentplatform\" == 'darwin'  && $restartinteractiverunner -eq 0 ]];", template);
                    
                    // 2. Check for proper nesting of if statements for node version checks
                    int nodeVersionCheckLines = 0;
                    bool foundNode24Block = false;
                    bool foundNode16Block = false;
                    bool foundNode12Block = false;
                    bool hasProperIndentation = false;
                    
                    string[] lines = template.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line.Contains("nodever=\"node24\""))
                        {
                            foundNode24Block = true;
                        }
                        if (line.Contains("nodever=\"node16\""))
                        {
                            foundNode16Block = true;
                        }
                        if (foundNode16Block && line.Contains("nodever=\"node12\""))
                        {
                            foundNode12Block = true;
                            // Check if we have proper indentation for this nested block
                            hasProperIndentation = line.StartsWith("                    ");
                        }
                        if (line.Contains("Fallback if RunnerService.js was started with"))
                        {
                            nodeVersionCheckLines++;
                        }
                    }
                    
                    // The template has node24 check but there's no "Fallback if RunnerService.js was started with node24" comment for it
                    // Only the node20, node16, and node12 sections have this comment
                    Assert.Equal(3, nodeVersionCheckLines);  // node20, node16, node12 
                    Assert.True(foundNode24Block, "Could not find node24 block");
                    Assert.True(foundNode16Block, "Could not find node16 block");
                    Assert.True(foundNode12Block, "Could not find node12 block");
                    Assert.True(hasProperIndentation, "node12 block is not properly indented");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception during test: {ex.ToString()}");
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
        
        private void ValidateCmdScriptTemplateSyntax(string templateName, bool shouldPass, Func<string, string> templateModifier = null)
        {
            try
            {
                using (var hc = new TestHostContext(this))
                {
                    // Arrange
                    string rootDirectory = Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), ".."));
                    string templatePath = Path.Combine(rootDirectory, "src", "Misc", "layoutbin", templateName);
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    string tempUpdatePath = Path.Combine(tempDir, Path.GetFileName(templateName).Replace(".template", ""));

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
                    template = template.Replace("_ROOT_FOLDER_", rootDirectory);
                    template = template.Replace("_EXIST_RUNNER_VERSION_", "2.300.0");
                    template = template.Replace("_DOWNLOAD_RUNNER_VERSION_", "2.301.0");
                    template = template.Replace("_UPDATE_LOG_", Path.Combine(tempDir, "update.log"));
                    template = template.Replace("_RESTART_INTERACTIVE_RUNNER_", "0");
                    
                    // Write the processed template to a temporary file
                    File.WriteAllText(tempUpdatePath, template);
                    
                    // Act - Check syntax using cmd with special flags:
                    // /v:on - Enable delayed environment variable expansion
                    // /f:off - Disable file name completion
                    // /e:on - Enable command extensions
                    // These flags help validate the syntax without fully executing the script
                    var process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c /v:on /f:off /e:on \"{tempUpdatePath}\" echo SyntaxCheckOnly && exit /b 0";
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    string errors = process.StandardError.ReadToEnd();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    // Check for mismatched parentheses in the file content
                    int openParenCount = template.Split('(').Length - 1;
                    int closeParenCount = template.Split(')').Length - 1;
                    bool hasMissingParenthesis = openParenCount != closeParenCount;
                    
                    // Check for unclosed quotes (simple check - not perfect but catches obvious errors)
                    int doubleQuoteCount = template.Split('"').Length - 1;
                    bool hasUnclosedQuotes = doubleQuoteCount % 2 != 0;

                    // Determine if the validation passed
                    bool validationPassed = process.ExitCode == 0 &&
                                          string.IsNullOrEmpty(errors) &&
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
