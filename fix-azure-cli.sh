#!/bin/bash

# Fix Azure CLI Installation Script
# This script will properly install/fix Azure CLI on your Mac

echo "🔧 Fixing Azure CLI Installation..."

# 1. First, let's clean up the broken installation
echo "🧹 Cleaning up broken Azure CLI installation..."
brew uninstall azure-cli 2>/dev/null || true

# 2. Clean up any leftover files
echo "🗑️ Removing leftover files..."
rm -rf /opt/homebrew/Cellar/azure-cli/ 2>/dev/null || true
rm -f /opt/homebrew/bin/az 2>/dev/null || true

# 3. Update Homebrew
echo "🔄 Updating Homebrew..."
brew update

# 4. Install Azure CLI fresh
echo "📦 Installing Azure CLI..."
brew install azure-cli

# 5. Verify installation
echo "✅ Verifying Azure CLI installation..."
if command -v az &> /dev/null; then
    echo "✅ Azure CLI installed successfully!"
    az version
else
    echo "❌ Azure CLI installation failed. Let's try alternative method..."
    
    # Alternative installation method using curl
    echo "🔄 Trying alternative installation..."
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
fi

# 6. Test basic command
echo "🧪 Testing Azure CLI..."
az --help | head -5

echo ""
echo "✅ Azure CLI setup complete!"
echo "🚀 Now you can run: az login"
echo "📁 Then continue with: ./setup-azure.sh"