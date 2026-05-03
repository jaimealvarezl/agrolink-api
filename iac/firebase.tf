# ── Auth providers ────────────────────────────────────────────────────────────

resource "google_identity_platform_config" "auth" {
  project = var.project_id

  sign_in {
    email {
      enabled           = true
      password_required = true
    }
  }

  depends_on = [google_project_service.apis]
}
