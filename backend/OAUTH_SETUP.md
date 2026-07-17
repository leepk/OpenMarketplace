# Google and Facebook OAuth

Production environment:

```env
Customer__BaseUrl=https://vunoca.com
```

Google authorized redirect URI:

```text
https://api.vunoca.com/api/v1/auth/external/google/callback
```

Facebook valid OAuth redirect URI:

```text
https://api.vunoca.com/api/v1/auth/external/facebook/callback
```

Configure credentials and enable providers in Admin > Site Settings > Authentication Providers.
The backend reads provider credentials from AppSettings at request time; no restart is required after saving settings.
