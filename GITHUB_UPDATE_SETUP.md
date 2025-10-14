# 🚀 GitHub Update System Setup for FehlzeitApp

This document explains how to set up automated updates for FehlzeitApp using GitHub Releases and Velopack.

## 📁 Repository Structure

Your GitHub repository should contain:

```
vsu-fehlzeit/
├── FehlzeitApp/
│   ├── .github/workflows/publish.yml
│   ├── velopack.yml
│   ├── FehlzeitApp.csproj
│   ├── FehlzeitApp.exe (built)
│   └── ... (other app files)
```

## ⚙️ Configuration Files

### 1. velopack.yml
```yaml
appId: "com.haniallam.fehlzeitapp"
appVersion: "1.0.0"
appName: "FehlzeitApp"
entryExecutable: "FehlzeitApp.exe"
publisherName: "Hani Allam"
url: "https://github.com/HaniAllamM/vsu-fehlzeit/releases/latest/download/"
```

### 2. GitHub Actions Workflow (.github/workflows/publish.yml)
- Automatically builds and packages your app when you push a version tag
- Creates GitHub releases with Velopack installers
- Supports manual triggering

## 🔄 How It Works

### 1. **Development Flow**
1. Make changes to your FehlzeitApp
2. Test locally
3. Commit and push changes
4. Create a version tag: `git tag v1.0.1 && git push origin v1.0.1`
5. GitHub Actions automatically builds and publishes the release

### 2. **User Update Flow**
1. User opens FehlzeitApp
2. App checks GitHub releases for updates
3. If update available, shows notification
4. User confirms → App downloads and installs update
5. App restarts with new version

## 🛠️ Setup Instructions

### Step 1: Initialize GitHub Repository
```bash
# In your FehlzeitApp folder
git init
git add .
git commit -m "Initial commit with update system"
git remote add origin https://github.com/HaniAllamM/vsu-fehlzeit.git
git push -u origin main
```

### Step 2: Create First Release
```bash
# Tag your current version
git tag v1.0.0
git push origin v1.0.0
```

### Step 3: Test the System
1. Make a small change to your app
2. Update version in `FehlzeitApp.csproj`:
   ```xml
   <AssemblyVersion>1.0.1.0</AssemblyVersion>
   <FileVersion>1.0.1.0</FileVersion>
   ```
3. Update `velopack.yml`:
   ```yaml
   appVersion: "1.0.1"
   ```
4. Create new tag:
   ```bash
   git add .
   git commit -m "Version 1.0.1"
   git tag v1.0.1
   git push origin v1.0.1
   ```

## 📱 Update Logic in Your App

Your `App.xaml.cs` already contains the update logic:

```csharp
// Check for updates before starting the app
_ = Task.Run(async () =>
{
    try
    {
        var updateUrl = "https://github.com/HaniAllamM/vsu-fehlzeit/releases/latest/download/";
        var mgr = new UpdateManager(updateUrl);
        var updateInfo = await mgr.CheckForUpdatesAsync();

        if (updateInfo != null)
        {
            var result = MessageBox.Show(
                $"A new version ({updateInfo.TargetFullRelease.Version}) is available!\n\n" +
                "Would you like to update now?",
                "Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                mgr.ApplyUpdatesAndRestart(updateInfo);
            }
        }
    }
    catch (Exception ex)
    {
        // Log error but don't block app startup
        File.AppendAllText("update.log", $"[{DateTime.Now}] Update check failed: {ex.Message}\n");
    }
});
```

## 🎯 Benefits

✅ **Automatic Updates**: Users get notified of new versions  
✅ **Secure**: HTTPS downloads from GitHub  
✅ **Reliable**: GitHub's infrastructure  
✅ **Version Control**: Each release is tagged and documented  
✅ **CI/CD**: Automated building and publishing  
✅ **Rollback**: Users can download previous versions if needed  

## 🔧 Troubleshooting

### Common Issues:

1. **Update not found**: Check that the GitHub URL is correct
2. **Build fails**: Ensure .NET 9.0 is specified in the workflow
3. **Permission denied**: Make sure GitHub token has release permissions
4. **App won't start after update**: Check that entryExecutable in velopack.yml matches your exe name

### Debug Steps:

1. Check GitHub Actions logs for build errors
2. Verify the release was created with the correct files
3. Test the update URL manually in a browser
4. Check the update.log file in your app directory

## 📋 Version Management

### Updating Your App Version:

1. **Update AssemblyInfo** in `FehlzeitApp.csproj`:
   ```xml
   <AssemblyVersion>1.0.2.0</AssemblyVersion>
   <FileVersion>1.0.2.0</FileVersion>
   ```

2. **Update velopack.yml**:
   ```yaml
   appVersion: "1.0.2"
   ```

3. **Create and push tag**:
   ```bash
   git tag v1.0.2
   git push origin v1.0.2
   ```

## 🚀 Next Steps

1. Push your code to GitHub
2. Create your first release tag
3. Test the update system
4. Distribute the initial installer to users
5. Future updates will be automatic!

---

**Note**: This system works best when users have the initial version installed via the Velopack installer, as it embeds the update URL for future checks.
