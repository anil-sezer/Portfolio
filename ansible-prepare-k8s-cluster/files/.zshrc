# Enable Powerlevel10k instant prompt. Should stay close to the top of ~/.zshrc.
# Initialization code that may require console input (password prompts, [y/n]
# confirmations, etc.) must go above this block; everything else may go below.
if [[ -r "${XDG_CACHE_HOME:-$HOME/.cache}/p10k-instant-prompt-${(%):-%n}.zsh" ]]; then
  source "${XDG_CACHE_HOME:-$HOME/.cache}/p10k-instant-prompt-${(%):-%n}.zsh"
fi

export ZSH="$HOME/.oh-my-zsh"

ZSH_THEME="powerlevel10k/powerlevel10k"

zstyle ':omz:update' mode auto
COMPLETION_WAITING_DOTS="true"

plugins=(
  git
  sudo
  extract
  command-not-found
  zsh-bat
  you-should-use
  # Community Plugins (must be last)
  zsh-autosuggestions
  zsh-syntax-highlighting
)

fpath+=${ZSH_CUSTOM:-${ZSH:-~/.oh-my-zsh}/custom}/plugins/zsh-completions/src
autoload -U compinit && compinit

source "$ZSH/oh-my-zsh.sh"

alias alias.edit="nano ~/.zshrc; source ~/.zshrc"
alias alias.reload="source ~/.zshrc"
alias showip="ip addr"

if (( $+commands[kubecolor] )); then
  alias k="kubecolor"
fi

if (( $+commands[kubectl] )); then
  source <(kubectl completion zsh)
  if (( $+commands[kubecolor] )); then
    compdef kubecolor=kubectl
    compdef k=kubectl
  fi
fi

if (( $+commands[kubeadm] )); then
  source <(kubeadm completion zsh)
fi

export KUBE_EDITOR=nano
export PATH="$PATH:$HOME/.local/bin"

# To customize prompt, run `p10k configure` or edit ~/.p10k.zsh.
[[ ! -f ~/.p10k.zsh ]] || source ~/.p10k.zsh
