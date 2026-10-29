# Quiz Application — Gamification and Learning Performance

## Overview

A Unity-based browser quiz developed for a bachelor's thesis investigating how gamification elements influence learning performance and user engagement during university exam preparation.

## Features

- 10 randomly selected multiple-choice questions
- Timer and progress indicator
- Optional points, stars, and progress bar
- Configurable sound effects
- Results showing score, completion time, and accuracy
- Optional quiz data collection via Google Apps Script and Google Sheets
- Feedback questionnaire

## Requirements

- Unity Editor **6000.5.6f1**
- WebGL Build Support
- Compatible web browser (tested with Microsoft Edge)

## Getting Started

1. Clone or download this repository.
2. Open Unity Hub and add the project from disk.
3. Open the project using the required Unity version.
4. Open the Main Menu scene and enter Play mode.

## Project Structure

| Directory | Description |
|---|---|
| `Assets/Animations/` | Animations |
| `Assets/Scenes/` | Application scenes |
| `Assets/Scripts/` | Quiz logic, UI, settings, and data collection |
| `Assets/QnA/` | CSV question bank |
| `Assets/SFX/` | Sound effects |
| `Packages/` | Package dependencies |
| `ProjectSettings/` | Unity project settings |

## Configuration

Configure URLs in `Assets/Scripts/AppConfig.cs`:

- `FeedbackFormUrl`: Feedback questionnaire URL
- `GoogleAppsScriptUrl`: Google Apps Script endpoint for quiz data

## Data Collection

Quiz results can be sent to Google Sheets through Google Apps Script. To enable collection, configure a compatible deployment and ensure its expected data fields match the application.

## Further Development

Developers can extend the application by adding questions to the CSV question bank, modifying the existing C# scripts, adjusting the user interface, or introducing additional gamification elements.
