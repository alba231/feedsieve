resource "google_project" "feed-sieve" {
  provider = google-beta

  name            = var.project_name
  project_id      = var.project_name

  labels = {
    "firebase" = "enabled"
  }
}
