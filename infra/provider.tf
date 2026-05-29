provider "google-beta" {
  project               = var.project_name
  billing_project       = var.project_name
  user_project_override = true
  region                = "europe-west1"
}