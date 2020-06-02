# ADS Bot - Standard

### Azure Infrastructure
Each ADS bot on azure (dev, prod) consists of the following resources.
 - Web App Bot
 - App Service (Runs the Bot)
 - Application Insights (logging & error reporting)

With the following resources being shared across both Dev and Prod environments.

 - App Service Plan - The actual "computer"/billable resource running everything.
 - Azure Function - Sync (Nightly @ Midnight)
 - Storage Account
   - Stores BB Sync Information
   - Stores User/Converstaion Data (Dev/Prod isolated)
 - LUIS Resource & Application
 - QnA Maker Resource & Application


### Deployment Process
Deployment of the bot is automated, just push to either `development` or `master` to deploy to either Development or Production instance respectively. The latest version will automatically be downloaded, compiled, and pushed up to Azure via Github actions.

Deployment of the Nightly Sync Azure Function is done via Visual Studio.
1. Download the Publish Profile from the relevant Azure Function (only one exists currently.)
2. Import the Publish Profile into Visual Studio from the Publish page.
3. `Publish` function project in Visual Studio.
4. Select imported profile.


### Data Storage
The bot uses a Storage Account to store everything.

 - BuyerBridge data is stored in the Table section, as a Cars and Dealers table.
 - User and Conversation data is stored in the BLOB section as many flat JSON files.


### Configuration

The bot uses many configuration sections, most are related to connections to external services.

These settings can be entered individually in Azure configuration pages for the Web App Bot itself, or by defining them in the appsettings.json file for fixed values.

These are the recognized config variables currently.

Setting Name | Description | Valid Values
--- | --- | --- |
ads:crm_enabled | Controls if creating CRM leads is enabled or not. Used for dev. | True/False
ads:debug_messages | Used to debug payloads and messages. | True/False
ads:debug_errors | Sends error traces in the chat itself, useful for debugging issues. | True/False
ads:name_regex | Controls the regex used to validate manualy-entered names. | Any RegEx Expressions
ads:environment | Controls the blob storage container name for user and conversation storage | prod-standard or dev-standard
bb:base | URL for BuyersBridge API | URL
bb:token | Token for BuyersBridge API | Auth Token
luis:id | ID for LUIS App | GUID
luis:endpoint | Endpoint for LUIS App | URL
luis:endpointKey | Key for LUIS App | Endpoint Key
qna:QnAKnowledgebaseId | KB ID for QnA App | GUID
qna:QnAEndpointKey | Key for QnA App | GUID
qna:QnAEndpointHostName | Hostname of QnA App | URL
zoho:email | Email zoho leads are created under | Email
zoho:client_id | Client ID of registered zoho credentials | OAuth ClientID
zoho:client_secret | Client Secret of registered zoho credentials | OAuth Client Secret
zoho:redirect_url | Redirect URL for OAuth flow. Fixed value | http://www.zoho.com/subscriptions
zoho:refresh_token | Refresh Token of Registered zoho credentials | OAuth Refresh Token