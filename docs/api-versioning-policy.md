# TMS API Versioning Policy

## What counts as a breaking change
A breaking change requires a new version. These are breaking:
- Removing a field from a response
- Renaming a field in a response or request
- Changing a status code (e.g. 200 → 204, 400 → 422)
- Tightening validation (e.g. making an optional field required)
- Changing the default sort order of a collection

## What counts as additive (non-breaking)
These changes do not require a new version:
- Adding a new optional field to a response
- Adding a new endpoint
- Adding a new optional query parameter
- Relaxing validation (e.g. making a required field optional)

## Sunset window
V1 runs for a minimum of **6 months** after V2 ships. Rural training centres on quarterly maintenance schedules must have time to migrate. V1 shutdown date is committed in the repo and communicated at least 6 months in advance.

## Communication
From day one of V2:
- Every V1 response carries `Deprecation: true`, `Sunset: <date>`, and `Link: </api/v2/...>; rel="successor-version"` headers
- A CHANGELOG entry is added
- An email is sent to every team that holds an API key
- A calendar invite is sent for the V1 shutdown date

## Skipping versions
V1 → V3 is allowed. Clients are not forced to migrate through every intermediate version. The successor `Link` header always points to the latest stable version.
