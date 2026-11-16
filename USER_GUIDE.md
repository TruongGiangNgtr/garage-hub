# GarageHub User Guide

GarageHub is an ASP.NET Core web application for managing vehicles, booking garage service slots, and paying a booking deposit through PayOS. This guide describes the features currently available in the application.

## What you can do

- Sign in with a verified Google account.
- Complete a profile for a first-time account.
- Add and manage your vehicles.
- Book a garage and one of the available service slots.
- Pay the booking deposit through PayOS and view your bookings on the home page.
- Use the **Admin Functions** password gate to create, edit, and delete garages.

Vehicle scanning is currently a user-interface placeholder. Reviews and maintenance reminders have database entities but no user-facing screens or workflows yet.

## Access and accounts

### Customer access

1. Select **Login with Google** on the home page.
2. Complete Google's sign-in process using an account with a verified email.
3. If the email or Google account is already registered, GarageHub signs you in and takes you to the booking page. The account name is refreshed from the Google profile when available.
4. On first login, enter your full name in the profile form. The email is supplied by Google and cannot be changed in that form.

The database assigns the internal user ID and creation time. New Google accounts receive the `Customer` role.

Use **Sign out** in the navigation to end a Google customer session.

### Admin Functions access

The **Admin Functions** button is available on the home page. Select it, enter the shared admin password, and submit the form to open Garage management. This password session is separate from Google accounts; it lasts up to 30 minutes.

The password is not included in the repository. The application reads it from the `ADMIN_PASSWORD` User Secret. Password submissions are rate-limited to five attempts per IP address in a five-minute window. Use **Exit** on the garage list to end the admin session and return to the Admin Functions password page.

Google accounts receive the `Customer` role by default. There is no user-facing screen for promoting Google accounts to `GarageOwner` or `Admin`. Garage management is also authorized for Google accounts that already have either of those roles.

## Managing vehicles

1. Sign in with Google.
2. Open **My vehicles** in the navigation.
3. Select **Add vehicle**, enter the plate number, brand, and model, then save.
4. Use **Edit** to update a vehicle or **Delete** to remove it.

Plate numbers must be unique across the system. A vehicle cannot be deleted while it has a booking or reminder associated with it. Vehicle records belong to the signed-in customer.

The scan button on the booking form does not upload or recognize a file. Select a saved vehicle from the vehicle list instead.

## Booking a service slot

1. Sign in and select **Add booking** or open the booking page.
2. Choose one of your vehicles and an available garage.
3. Select a date and one of the fixed time slots:
   - 08:00–10:00
   - 10:00–12:00
   - 13:00–15:00
   - 15:00–17:00
4. Select **Continue to checkout**.
5. Complete the deposit payment on PayOS.

Times are handled as Vietnam local time (UTC+7). The selected slot must be in the future. Each garage has a capacity per slot; the sample garages are seeded with a capacity of two bookings per slot. Pending bookings hold capacity for 15 minutes. Expired pending bookings no longer count toward the slot capacity.

The booking page requires at least one saved vehicle and one available garage. If you have no vehicle, use the **Add vehicle** link shown on the page before booking.

## Deposit payment and booking status

The current deposit is **50,000 VND**. A booking starts as `Pending` while payment is being completed. PayOS confirms a payment through its verified webhook; GarageHub also checks payment status directly before treating a payment as confirmed.

After returning from PayOS, the result page displays the payment and booking state. A pending result refreshes automatically while the system waits for confirmation. If PayOS confirms cancellation, the pending payment is canceled and the held slot is released. If payment-link creation fails, GarageHub cancels the pending payment before surfacing the error.

The signed-in home page shows your booking schedule, including its date/time, vehicle plate, garage, status, and deposit. The current customer interface does not provide a booking edit or cancellation action.

## Garage management

After entering the Admin Functions password, the garage list provides **Add garage**, **Edit**, and **Delete** actions.

Garage fields:

- **Name**, **Address**, and **Phone** are required.
- **Owner** is optional. If selected, it must be an existing user.
- **Capacity per slot** must be at least one.
- **Average rating** is displayed but is not editable in the garage form.

A garage with bookings cannot be deleted. Garage CRUD is also protected by the `GarageOwner` or `Admin` role when using a Google-authenticated account.

## Local setup for developers and operators

### Prerequisites

- .NET 10 SDK
- SQL Server
- Google OAuth web client credentials
- PayOS credentials for creating and confirming real deposits

### Initialize SQL Server

The repository includes [`GarageHub.Database.sql`](./GarageHub.Database.sql). Open it in SQL Server Management Studio and execute it against the SQL Server instance used by the application's connection string. It creates the `GarageHub` database when needed, applies the initial schema, and inserts three sample garages. The script does not drop an existing database; review and back up existing data before running database scripts.

### Configure secrets

Keep actual credentials in User Secrets, not in `appsettings.json` or source control. From the repository root, configure the settings required for the features you intend to use:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL Server connection string>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_ID" "<Google OAuth client ID>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_SECRET" "<Google OAuth client secret>" --project src\GarageHub.Web
dotnet user-secrets set "ADMIN_PASSWORD" "<choose a strong, unique password>" --project src\GarageHub.Web
```

For PayOS payments, also configure:

```powershell
dotnet user-secrets set "APP_PUBLIC_BASE_URL" "<public HTTPS base URL>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CLIENT_ID" "<PayOS client ID>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_API_KEY" "<PayOS API key>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CHECKSUM_KEY" "<PayOS checksum key>" --project src\GarageHub.Web
```

`APP_PUBLIC_BASE_URL` must be reachable by PayOS for the configured return and cancel URLs. The webhook endpoint is `POST /api/payments/payos-callback`.

The project User Secrets ID is `fafbc8e2-a2dc-469a-b2f8-0eae33b824fb`. On Windows, the secrets file is:

```text
%APPDATA%\Microsoft\UserSecrets\fafbc8e2-a2dc-469a-b2f8-0eae33b824fb\secrets.json
```

You may edit that file directly, preserving its JSON structure and existing secret entries. Do not copy secret values into this guide or commit the secrets file.

### Configure Google OAuth and start the app

For the HTTPS launch profile, add this exact authorized redirect URI to the Google OAuth web client:

```text
https://localhost:7100/signin-google
```

From the repository root, start the HTTPS profile:

```powershell
dotnet run --project src\GarageHub.Web --launch-profile https
```

The launch settings also define an HTTP profile at `http://localhost:5133`. Use the HTTPS profile for the redirect URI above. The browser may prompt you to trust the local development HTTPS certificate.

## Troubleshooting

- **Google redirect URI mismatch:** Check that the URI registered in Google Cloud exactly matches the scheme, host, port, and `/signin-google` path of the profile you launched.
- **Error after Google redirects back to GarageHub:** Check the web app console for the exception. Confirm the SQL Server is running, the connection string points to that instance, and the database schema has been initialized.
- **Admin password page says access is not configured:** Set `ADMIN_PASSWORD` in User Secrets and restart the app.
- **Booking form has no vehicles:** Add a vehicle first.
- **No garages appear:** Initialize the database or use Admin Functions to add a garage.
- **PayOS checkout fails:** Confirm the PayOS secrets are configured and `APP_PUBLIC_BASE_URL` is a valid, publicly reachable HTTPS address.
- **A vehicle or garage cannot be deleted:** Vehicles with bookings or reminders and garages with bookings are protected from deletion.

## Features not available in the current user interface

- OCR plate recognition and image/document upload.
- Customer booking edit or cancellation.
- Review submission and maintenance reminder management.
- User-facing role assignment for `GarageOwner` or `Admin`.
