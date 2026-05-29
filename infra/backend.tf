terraform {
  backend "azurerm" {
    resource_group_name  = "feed-sieve"
    storage_account_name = "feedsieve"
    container_name       = "terraform-state"
    key                  = "terraform_state.tfstate"
  }
}