# AITrayApp Developer Documentation

## Overview
AITrayApp is a C# desktop application that provides AI-based text transformation and generation features via a system tray interface. It utilizes the Gemini API for natural language processing tasks. The application stores user settings such as API keys and custom prompts securely and allows interaction through a context menu on the tray icon.

---

## Components Breakdown

### **SettingsManager**
This static class handles secure storage and retrieval of user settings, such as API keys and custom prompts.

#### **Key Methods**:
1. **SaveApiKey(string apiKey)**:
   - Encrypts and saves the API key to a file (`settings.bin`) using Windows Data Protection API (DPAPI).

2. **GetApiKey()**:
   - Retrieves and decrypts the stored API key.

3. **SaveCustomPrompt(string customPrompt)**:
   - Encrypts and saves a custom prompt to a file (`settings2.bin`) using DPAPI.

4. **GetCustomPrompt()**:
   - Retrieves and decrypts the stored custom prompt.

#### **Security**:
- Uses `DataProtectionScope.CurrentUser` to ensure encryption is tied to the current user account.
- Stores settings in `%LocalAppData%\AITrayApp`.

---

### **GeminiApi**
This class facilitates communication with the Gemini API to generate AI-driven text transformations.

#### **Constructor**:
- `GeminiApi(string apiKey)`:
  - Validates the provided API key.
  - Initializes an `HttpClient` with the Gemini API base URL.

#### **Key Method**:
- **GenerateText(string prompt, string modelName = "gemini-1.5-flash")**:
  - Sends a POST request to the Gemini API.
  - Serializes the request body using `System.Text.Json`.
  - Handles JSON responses to extract and return generated text.

#### **Error Handling**:
- Handles network errors (`HttpRequestException`) and JSON parsing errors (`JsonException`).
- Provides user feedback via `MessageBox` for unexpected issues.

#### **Response Parsing**:
- Extracts generated text from nested JSON response structures.

---

### **TrayApplicationContext**
Manages the system tray icon, context menu, and user interactions.

#### **Initialization**:
1. **Tray Icon**:
   - Displays an icon in the system tray.
   - Double-click triggers the Settings dialog.

2. **Context Menu**:
   - Provides options for various text transformations (e.g., Rewrite, Summarize, Custom).
   - Settings and Exit options.

#### **Key Features**:
- **Clipboard Integration**:
  - Uses `Clipboard.ContainsText()` to check for text.
  - Processes clipboard text through Gemini API and replaces it with AI-transformed content.

- **Menu Items**:
  - Each transformation option invokes `TalkToGemini()` with a predefined prompt.

- **Settings Management**:
  - Opens `SettingsForm` for API key and custom prompt configuration.

#### **Custom Prompts**:
- Allows users to define custom instructions for text transformations.

#### **Icon Management**:
- Dynamically loads the application icon from the executable or uses a system icon as a fallback.

#### **Functions**:

**RewriteItem_Click, SpellItem_Click, ProItem_Click, FriendlyItem_Click, SummarizeItem_Click, LongerItem_Click, ExtendItem_Click, ShortItem_Click**
- Purpose: Handle clicks on specific context menu items.
- Details:
  - Define task-specific prompts and append the clipboard text to the prompt.
  - Call TalkToGemini with the constructed prompt.
- Called From: User interactions with context menu items.

**CustomItem_Click**
- Purpose: Sends the user-defined custom prompt to the Gemini API.
- Details:
  - Appends clipboard content to the stored custom prompt.
  - Calls TalkToGemini.
- Called From: User interactions with the context menu.

**SettingsItem_Click**
- Purpose: Opens the SettingsForm dialog to update settings.
- Details:
  - Saves updated API key and custom prompt using SettingsManager.
- Called From: User interaction with the Settings menu item.

**ExitItem_Click**
- Purpose: Exits the application.
- Details:
  - Hides the tray icon and terminates the application.
  - Called From: User interaction with the Exit menu item.

---

### **SettingsForm**
A modal dialog for configuring user settings.

#### **UI Components**:
1. **TextBoxes** for API Key and Custom Prompt.
2. **Save and Cancel Buttons** for user actions.

#### **Behavior**:
- Validates inputs and saves settings via `SettingsManager`.

---

## Key Application Flow

1. **Startup**:
   - Loads API key and custom prompt from settings.
   - Initializes the tray icon and context menu.

2. **User Interaction**:
   - User selects a context menu option.
   - Application fetches clipboard text, sends it to Gemini API, and updates the clipboard with the transformed text.

3. **Settings Management**:
   - Opens a modal dialog for modifying settings.

4. **Secure Storage**:
   - Encrypts user inputs and stores them locally.

