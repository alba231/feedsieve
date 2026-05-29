terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    google-beta = {
      source  = "hashicorp/google-beta"
      version = "~> 6.0"
    }
  }

  required_version = "~> 1.14"
}

module "firebase" {
  source = "./modules/firebase"
  project_name = var.project_name
}