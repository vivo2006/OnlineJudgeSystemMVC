# Security notes

Real API/SMTP credentials were present in earlier commits. If those credentials were valid, revoke/rotate them through RapidAPI and the email provider before making this application public. Removing them from the current files does not remove their exposure through Git history or clones.

Local development credentials belong in the ignored `OnlineJudgeSystemMVC/appsettings.Local.json`, with access limited to your local account. The web host loads this file only in Development. Production must receive secrets from environment variables or a deployment secret store. Do not upload the local file with a deployment artifact.

The administrator password is supplied explicitly during seeding. Do not reuse the previously hardcoded password, and do not expose administrator credentials in a public demo. Existing administrator accounts are not silently changed by the seeder.

Use HTTPS. Keep code execution on an isolated judge service. Apply migrations and review any old Hangfire jobs explicitly before enabling the upgraded worker. API errors are not retried automatically: an ambiguous POST failure may already have created provider work.

The application limits daily submission attempts and uses a single delayed result check. Limits count application batches; they cannot guarantee a provider allowance because subscriptions may count individual test executions. Configure conservative limits for the actual plan.

The regression suite checks judging/scoring, ownership and path-validation behavior. It is not a complete penetration test. Do not publish discovered credentials or sensitive data in GitHub issues.
