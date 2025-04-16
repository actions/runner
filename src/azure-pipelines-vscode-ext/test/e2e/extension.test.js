const assert = require('assert');
const vscode = require('vscode');
const path = require('path');
const fs = require('fs');

suite('Extension Test Suite', () => {
  vscode.window.showInformationMessage('Start all tests.');

  test('Extension is activated', async () => {
    const extension = vscode.extensions.getExtension('christopherhx.azure-pipelines-vscode-ext');
    assert.ok(extension, 'Extension is not found');
    await extension?.activate();
    assert.strictEqual(extension?.isActive, true, 'Extension is not active');
  });

  test('Command is registered', async () => {
    const commands = await vscode.commands.getCommands(true);
    assert.ok(commands.includes('azure-pipelines-vscode-ext.expandAzurePipeline'), 'Command is not registered');
  });

  test('Open YAML file and assert context menu', async () => {
    const yamlFilePath = path.join(__dirname, 'test-pipeline.yml');
    const yamlContent = 'name: Test Pipeline\nsteps:\n  - script: echo Hello, World!\n    displayName: \'Run a script\'\n';

    // Create a temporary YAML file
    fs.writeFileSync(yamlFilePath, yamlContent);

    try {
      // Open the YAML file in the editor
      const document = await vscode.workspace.openTextDocument(yamlFilePath);
      const editor = await vscode.window.showTextDocument(document);

      // Assert the editor is open
      assert.strictEqual(editor.document.fileName, yamlFilePath, 'YAML file is not opened');

      const changeTextEditor = new Promise((resolve, reject) => {
        vscode.workspace.onDidChangeTextDocument((e) => {
          try {
            const visibleEditor = vscode.window.visibleTextEditors.find(editor => editor.document.fileName === yamlFilePath);
            assert.ok(visibleEditor, 'YAML file is not visible in the editor');
            const previewEditor = vscode.window.visibleTextEditors.find(editor => editor.document.uri.scheme === "azure-pipelines-vscode-ext");
            assert.ok(previewEditor, 'YAML previewEditor is not visible in the editor');

            if(e.document.uri.scheme === "azure-pipelines-vscode-ext") {
              const text = e.document.getText();
              assert.match(text, /task: CmdLine@2/, 'YAML previewEditor does not contain expected text');
              resolve();
            }
          } catch (error) {
            reject(error);
          }
        });
      });

      await vscode.commands.executeCommand('azure-pipelines-vscode-ext.expandAzurePipeline', vscode.Uri.file(path.dirname(yamlFilePath)), true);

      await changeTextEditor;

      vscode.languages.getDiagnostics(document.uri).filter(diagnostic => diagnostic.severity === vscode.DiagnosticSeverity.Error).forEach(diagnostic => {
        assert.fail(`Unexpected error in YAML file: ${diagnostic.message}`);
      });

      // Add any additional assertions for the context menu behavior here
      vscode.window.showInformationMessage('Context menu command executed successfully.');
    } finally {
      // Clean up the temporary YAML file
      fs.unlinkSync(yamlFilePath);
    }
  }).timeout(10000);
});