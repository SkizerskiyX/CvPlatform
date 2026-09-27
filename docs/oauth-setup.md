# Google and Facebook sign-in on Render

Production site: https://cvplatform-h1rj.onrender.com

OAuth is disabled by default (`OAuth:Enabled=false`). Deploy without Google/Facebook keys: the provider handlers and external cookie are not registered, and social login buttons stay hidden. Regular registration supports the optional `isRecruiter` boolean (false = Candidate, true = Recruiter); this is public self-registration with no approval step. Admin cannot be selected.

To enable social login later, set `OAuth__Enabled=true` in Render and provide credentials below.

## Provider dashboards

Google Cloud / Google Auth Platform:
1. Configure branding, support contact and an External audience.
2. Create an OAuth client with application type **Web application**.
3. Add this exact authorized redirect URI:
   `https://cvplatform-h1rj.onrender.com/signin-google`
4. Obtain the client ID and secret. Configure test users while testing, then publish the app for public use according to the dashboard requirements.

Meta for Developers:
1. Create an app with Facebook Login for the web.
2. Configure the site URL as `https://cvplatform-h1rj.onrender.com`.
3. Add this exact Valid OAuth Redirect URI:
   `https://cvplatform-h1rj.onrender.com/signin-facebook`
4. Enable the `email` permission and obtain the app ID and secret.
5. Complete Meta's publication requirements (including privacy policy and user data deletion information) and required permission access/review for users outside app roles. Test with app testers before making it public.

The provider redirects to `/signin-google` or `/signin-facebook` on the API. `/oauth-callback` is the SPA's final destination and must not be used as the provider redirect URI.

## Render environment variables

Enter each name and value in separate dashboard fields. Do not paste `NAME=value` into the Value field.

| Name | Value |
| --- | --- |
| `OAuth__Enabled` | `true` only when enabling social login |
| `OAuth__Google__ClientId` | Google OAuth client ID |
| `OAuth__Google__ClientSecret` | Google OAuth client secret |
| `OAuth__Facebook__AppId` | Meta app ID |
| `OAuth__Facebook__AppSecret` | Meta app secret |
| `OAuth__PublicOrigin` | `https://cvplatform-h1rj.onrender.com` |
| `Frontend__BaseUrl` | `https://cvplatform-h1rj.onrender.com` |
| `Cors__Origins__0` | `https://cvplatform-h1rj.onrender.com` |

The last three values are already production defaults. Environment variables override them, so remove or replace any old localhost values in Render. Use the Production ASP.NET Core environment. Existing database and JWT settings are still required. Never store provider secrets in Git or `VITE_*` variables.

Deploy the changes and restart after adding credentials. Each provider's button appears on both sign-in and registration only when its ID and secret are configured. `/api/account/external-providers` lists enabled providers without exposing credentials.

The server uses a configured trusted public origin to construct matching HTTPS OAuth callbacks behind Render's proxy; it does not trust arbitrary forwarded host headers.

## Behavior and checks

- New external identities create a Candidate account and profile, with no local password or email notification.
- Returning users are found by provider plus provider user ID, even if their provider email changes.
- Matching email addresses do not automatically link accounts. Existing users must use their original login method; account linking is not implemented.
- Missing email, cancelled consent, provider failures, and locked accounts show an error. No synthetic email or administrator/recruiter role is assigned.
- Account, external login, role and profile creation are committed in one database transaction.
- The short-lived external cookie is cleared after the callback. The SPA removes the JWT fragment from browser history immediately after reading it.

After deployment, test each provider: new user, returning user, cancelled consent, existing local email collision, and missing Facebook email. Confirm a new user receives Candidate, not Recruiter or Admin. Real provider login requires valid credentials and dashboard configuration; a local build cannot verify it.

Run the isolated controller regression checks with:
`dotnet run --project tests/CvPlatform.OAuthChecks -p:SpaBuildEnabled=false`.
These use fake Identity services and transactions; they do not contact a database or provider and do not replace end-to-end browser verification.

For local OAuth testing use the API's HTTPS launch profile (`https://localhost:7148`), register the corresponding `/signin-google` and `/signin-facebook` URIs where accepted by the provider, and set `VITE_API_URL=https://localhost:7148`. Keep `Frontend__BaseUrl=http://localhost:5173`; leave `OAuth__PublicOrigin` unset in Development. Trust the local development HTTPS certificate. For providers requiring a public HTTPS URL, use a separately configured development deployment.
