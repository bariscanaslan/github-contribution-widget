# GitHub Contribution Widget

A desktop widget that displays your GitHub contribution graph on your Windows desktop.

## Features

- Modern, transparent widget design

- Real-time GitHub contribution graph

- Runs on Windows startup

- Auto-refresh every hour

- System tray integration

## Prerequisites

- .NET 8 Runtime (or use self-contained build)

- GitHub Personal Access Token

## Setup

### 1. Clone the repository

```bash

git clone https://github.com/bariscanaslan/github-contribution-widget.git

cd github-contribution-widget

```

### 2. Configure your GitHub credentials

Create appsettings.json:

```json

{

  "GitHub": {

    "Token": "ghp_your_token_here",

    "Username": "your_username"

  }

}

```

Get your GitHub token from: https://github.com/settings/tokens

Required scopes: read:user

### 3. Build and Run

```bash

dotnet build

dotnet run

```

## Project Structure

```

├── App.xaml              # Application entry point

├── MainWindow.xaml       # Main widget UI

├── MainWindow.xaml.cs    # Widget logic

├── appsettings.example.json  # Config template

└── icon.ico              # Application icon

```

## Configuration

Edit appsettings.json:

```json

{

  "GitHub": {

    "Token": "your_github_token",

    "Username": "your_username"

  }

}

```

## Screenshots


## Author

Barış Can Aslan - [bariscanaslan.dev](https://bariscanaslan.dev)