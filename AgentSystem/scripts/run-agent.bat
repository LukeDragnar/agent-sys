@echo off
REM Usage: run-agent.bat <agent-type> "<task>"
REM Example: run-agent.bat git-agent "push branch thiennq/xxx"

set AGENT=%1
set TASK=%2
set BASE=D:\PROJECT\Git\Tool\agent-system

if "%AGENT%"=="" (
    echo Usage: run-agent.bat ^<agent-type^> "^<task^>"
    echo Available agents: git-agent, code-agent, review-agent
    exit /b 1
)

REM Build system prompt from agent + rules files
set AGENT_FILE=%BASE%\agents\%AGENT%.md
if not exist "%AGENT_FILE%" (
    echo Agent not found: %AGENT_FILE%
    exit /b 1
)

claude --print "%TASK%" --system-prompt-file "%AGENT_FILE%"
