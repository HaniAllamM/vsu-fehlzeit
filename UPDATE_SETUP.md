# Update System Setup

## Overview
Your FehlzeitApp now has an integrated update system using Velopack that checks for updates before the app starts.

## How It Works

### 1. **Startup Update Check**
- When the app starts, it automatically checks for updates
- If an update is available, it shows a dialog asking if you want to update
- If you choose "Yes", it downloads and installs the update, then restarts the app

### 2. **Manual Update Check**
- You can also manually check for updates using the "Check Updates" button in the main window
- This will show the same update dialog if an update is available

### 3. **Current Implementation**
- Currently set up as a **placeholder/demo** system
- Shows a simulation dialog to demonstrate the update process
- You can test the update flow without needing a real update server

## Configuration

### Update Server URL
Edit `FehlzeitApp/Services/UpdateService.cs` and change:
```csharp
_updateUrl = "https://your-update-server.com/updates";
```
Replace with your actual update server URL.

### Real Velopack Integration
To implement real Velopack updates, replace the placeholder code in `CheckForUpdatesAsync()` with:

```csharp
using var updateManager = new UpdateManager(_updateUrl);
var updateInfo = await updateManager.CheckForUpdatesAsync();

if (updateInfo != null)
{
    // Show update dialog
    var result = MessageBox.Show(
        $"Eine neue Version ({updateInfo.TargetFullRelease.Version}) ist verfügbar!\n\n" +
        $"Aktuelle Version: {updateInfo.CurrentVersion}\n" +
        $"Neue Version: {updateInfo.TargetFullRelease.Version}\n\n" +
        "Möchten Sie jetzt aktualisieren?",
        "Update verfügbar",
        MessageBoxButton.YesNo,
        MessageBoxImage.Information);

    if (result == MessageBoxResult.Yes)
    {
        // Download and install update
        await updateManager.DownloadUpdatesAsync(updateInfo);
        updateManager.ApplyUpdatesAndRestart(updateInfo);
        return true;
    }
}
```

## Testing the Update System

1. **Run the app
2. **Click "Check Updates" button** (or wait for startup check)
3. **Choose "Yes"** in the update dialog
4. **Watch the progress bar** simulate the download
5. **See the completion message**

## Files Created/Modified

- `FehlzeitApp/Services/UpdateService.cs` - Main update service
- `FehlzeitApp/Views/UpdateProgressDialog.xaml` - Progress dialog UI
- `FehlzeitApp/Views/UpdateProgressDialog.xaml.cs` - Progress dialog code
- `FehlzeitApp/App.xaml.cs` - Modified to check for updates at startup
- `FehlzeitApp/Views/MainWindow.xaml.cs` - Modified to handle update button

## Next Steps

1. **Set up your update server** (e.g., using Velopack's server tools)
2. **Replace the placeholder code** with real Velopack API calls
3. **Test with real updates** by publishing new versions
4. **Configure your update distribution** strategy

The update system is now ready to use! 🚀
