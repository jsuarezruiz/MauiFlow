# Expanded App Settings Documentation

## Overview
The MauiFlow app now includes expanded settings functionality that allows users to customize their experience through theme selection and other configurable options.

## Features Implemented

### 1. Theme Management
- **System Theme**: Follows the OS-level theme preference
- **Light Theme**: Forces light mode regardless of system setting
- **Dark Theme**: Forces dark mode regardless of system setting
- **Immediate Application**: Theme changes apply instantly without requiring app restart
- **Persistence**: Theme preferences are saved and restored across app sessions

### 2. App Configuration
- **Default Project Path**: Users can set a default path for project files
- **Settings Persistence**: All app settings are saved locally using secure storage
- **Validation**: Input validation ensures settings are properly configured

### 3. User Interface
- **Tabbed Settings**: Clean tabbed interface separating Azure OpenAI settings from app settings
- **Visual Feedback**: Active tab is highlighted for clear navigation
- **Responsive Design**: Settings panel adapts to the existing popup design

## Technical Implementation

### Models
- `AppTheme` enum: Defines available theme options (System, Light, Dark)
- `AppConfiguration` class: Contains app-level settings (theme, project path)
- `LLMConfiguration` class: Existing model for Azure OpenAI settings (unchanged)

### Services
- `ThemeService`: Manages theme application using `Application.Current.UserAppTheme`
- `SettingsService`: Extended to handle both LLM and app configuration persistence
- Uses `SecureStorage` for secure local persistence

### ViewModels
- `SettingsViewModel`: Enhanced with app settings management
- Immediate theme application when user changes selection
- Command pattern for all user actions (save, browse, test connection)

### Views
- `SettingsView`: Redesigned with tabbed interface
- Tab switching functionality with visual feedback
- Theme picker with bound selection handling

## Usage

### Accessing Settings
1. Click the settings icon in the main toolbar
2. Settings popup opens with two tabs:
   - **Azure OpenAI**: Configure API connection settings
   - **App Settings**: Configure theme and project preferences

### Changing Theme
1. Navigate to "App Settings" tab
2. Select desired theme from dropdown:
   - System (follows OS theme)
   - Light (always light mode)
   - Dark (always dark mode)
3. Theme applies immediately
4. Click "Save App Settings" to persist the change

### Setting Default Project Path
1. Navigate to "App Settings" tab
2. Enter path manually or click "Browse" button
3. Click "Save App Settings" to persist the change

## File Structure
```
src/
├── Models/
│   ├── AppTheme.cs (new)
│   ├── AppConfiguration.cs (new)
│   └── LLMConfiguration.cs (existing)
├── Services/
│   ├── ThemeService.cs (new)
│   └── SettingsService.cs (extended)
├── ViewModels/
│   └── SettingsViewModel.cs (enhanced)
├── Views/
│   └── SettingsView.xaml/.cs (redesigned)
└── Converters/
    └── ThemeToThemeOptionConverter.cs (new)
```

## Benefits
- **Immediate Feedback**: Theme changes apply instantly
- **Persistent Settings**: All preferences saved across sessions
- **Clean UI**: Organized tabbed interface
- **Extensible**: Easy to add more app settings in the future
- **User-Friendly**: Intuitive navigation and visual feedback