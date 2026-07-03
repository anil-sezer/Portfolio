#!/bin/bash

# scp ./common.sh husoyo@78.189.164.18:/home/husoyo/scripts/common.sh

# THIS FILE IS NOT MEANT TO BE RUN DIRECTLY. IT IS A COMMON FILE FOR OTHER SCRIPTS.

# ANSI Color Codes
RED='\033[0;31m'       # Error messages
GREEN='\033[0;32m'     # Success messages
YELLOW='\033[0;33m'    # Warnings
BLUE='\033[0;34m'      # Information
NC='\033[0m'           # No Color (Reset)

colored_echo() {
  local color=$1  # Store first argument as color
  shift           # Remove color argument

  for line in "$@"; do
    echo -e "${color}${line}${NC}"
  done
}

install_nfs_client() {

  if command -v apt &>/dev/null; then
    colored_echo $BLUE "Installing nfs-common if its not installed..."
    sudo apt update >/dev/null 2>&1 && sudo apt install -y nfs-common >/dev/null 2>&1
  else
    colored_echo $RED "Error: Unsupported OS. Install nfs-common manually."
    exit 1
  fi
}

starline() {
  colored_echo "$1" "****************************************"
}

print_header() {
  CYAN='\033[1;36m'
  NC='\033[0m' # No Color

  echo -e "${CYAN}"
  cat << "EOF"
 ______     __   __     __     __        
/\  __ \   /\ "-.\ \   /\ \   /\ \       
\ \  __ \  \ \ \-.  \  \ \ \  \ \ \____  
 \ \_\ \_\  \ \_\\"\_\  \ \_\  \ \_____\ 
  \/_/\/_/   \/_/ \/_/   \/_/   \/_____/ 
  
 ______     ______     ______     __  __     __  __     ______  
/\  == \   /\  __ \   /\  ___\   /\ \/ /    /\ \/\ \   /\  == \ 
\ \  __<   \ \  __ \  \ \ \____  \ \  _"-.  \ \ \_\ \  \ \  _-/ 
 \ \_____\  \ \_\ \_\  \ \_____\  \ \_\ \_\  \ \_____\  \ \_\   
  \/_____/   \/_/\/_/   \/_____/   \/_/\/_/   \/_____/   \/_/   
                                         
EOF
  echo -e "${NC}"
}

send_telegram_notification() {
    local message="$1"
    local status="$2"  # success, error, or info
    local hostname=$(hostname)

    # Get Telegram Bot Token and Chat ID from environment variables
    local token="${TELEGRAM_BOT_TOKEN}"
    local chat_id="${TELEGRAM_CHAT_ID}"

    if [ -z "$token" ] || [ -z "$chat_id" ]; then
        colored_echo "$YELLOW" "Warning: TELEGRAM_BOT_TOKEN or TELEGRAM_CHAT_ID is not set. Cannot send Telegram notification."
        return 1
    fi

    local emoji=""
    case "$status" in
        "success")
            emoji="✅"
            ;;
        "error")
            emoji="❌"
            ;;
        "info")
            emoji="ℹ️"
            ;;
        *)
            emoji="🔔"
            ;;
    esac

    # Send message using apprise
    apprise -t "Backup Status - Hostname: ${hostname}" -b "${emoji} ${message}" "tgram://${token}/${chat_id}"
}

run_command() {
    local cmd_output
    local cmd_status
    
    # Capture both stdout and stderr
    cmd_output=$("$@" 2>&1)
    cmd_status=$?

    if [ $cmd_status -ne 0 ]; then
        # Send error notification with command output
#        send_discord_notification "❌ Command failed: \`$1\`\n\nError output:\n\`\`\`\n$cmd_output\n\`\`\`" "error"
        send_telegram_notification "Command failed: \`$1\`\n\nError output:\n\`\`\`\n$cmd_output\n\`\`\`" "error"
        return 1
    fi
    return 0
}


