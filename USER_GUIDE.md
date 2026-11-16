# GarageHub User Guide

GarageHub is a garage booking and vehicle management web application built with ASP.NET Core MVC. It helps customers sign in with Google, register their vehicles, reserve a fixed service slot at a garage, pay a hold deposit through PayOS, and track the booking status from the home page.

This guide describes the project as it exists in the current codebase and covers setup, common flows, business rules, and the parts that are still intentionally not exposed in the UI.

## 1. What the project does

The current implementation includes:

- Google sign-in for customer accounts
- First-time profile completion for new users
- Vehicle CRUD for logged-in customers
- Garage listing and garage management for authorized staff/admin users
- Booking creation using fixed daily slots
- Pending booking hold + PayOS payment link creation
- Booking status tracking after returning from PayOS
- OCR-based license plate matching through the booking form
- Admin password gate for garage management

The application does not yet expose user-facing flows for:

- editing or canceling a booking from the customer UI
- review submission after service completion
- reminder management and maintenance calendars
- assigning GarageOwner or Admin roles from a screen

## 2. Role model and access

GarageHub uses cookie authentication with a custom user table. It does not use ASP.NET Core Identity.

Supported roles in the codebase:

- Customer: default role for any Google account created through the app
- GarageOwner: allowed to manage garages behind the same authorization checks as admin
- Admin: allowed to manage garages and also used for the password-based admin screen

Access flows:

- Regular customers sign in with Google and use the main website routes
- Garage management pages require either the authenticated Google user to be in the `GarageOwner` or `Admin` role, or a valid password session created via the Admin Functions page
- The admin password flow is separate from the Google sign-in cookie and uses a dedicated `GarageHub.Admin` cookie

## 3. Current project structure

The repo is organized as follows:

- src/GarageHub.Web
  - ASP.NET Core MVC app
  - Controllers, Razor views, static assets, launch settings, appsettings
- src/GarageHub.Service
  - business logic for users, vehicles, garages, bookings, payments, OCR
- src/GarageHub.Repository
  - EF Core DbContext, entities, repository abstractions, migrations
- src/GarageHub.Common
  - DTOs, enums, constants, shared helpers used across layers
- tests/GarageHub.Tests
  - automated tests for the solution

Main controllers in the web layer:

- AccountController: Google login, profile completion, sign-out
- VehiclesController: vehicle management
- BookingsController: booking form and checkout flow
- GaragesController: CRUD for garages
- PaymentController: PayOS return/cancel pages
- AdminController: password gate and admin session creation
- OcrApiController: upload endpoint for plate scanning
- HomeController: landing page and schedule summary

## 4. Requirements and prerequisites

You need the following to run the app successfully:

- .NET 10 SDK
- SQL Server instance that allows a database connection
- Google OAuth web client with a valid Client ID and Client Secret
- PayOS account and API credentials
- Plate Recognizer API token for OCR scanning

The project reads secrets from user-secrets rather than from source-controlled config files.

## 5. Setup instructions

### 5.1 Configure User Secrets

Run these commands from the repository root:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<SQL Server connection string>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_ID" "<Google OAuth client ID>" --project src\GarageHub.Web
dotnet user-secrets set "GOOGLE_CLIENT_SECRET" "<Google OAuth client secret>" --project src\GarageHub.Web
dotnet user-secrets set "ADMIN_PASSWORD" "<strong admin password>" --project src\GarageHub.Web
dotnet user-secrets set "APP_PUBLIC_BASE_URL" "https://localhost:7100" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CLIENT_ID" "<PayOS client ID>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_API_KEY" "<PayOS API key>" --project src\GarageHub.Web
dotnet user-secrets set "PAYOS_CHECKSUM_KEY" "<PayOS checksum key>" --project src\GarageHub.Web
dotnet user-secrets set "PLATE_RECOGNIZER_API_TOKEN" "<Plate Recognizer token>" --project src\GarageHub.Web
```

Notes:

- `APP_PUBLIC_BASE_URL` must be reachable by PayOS for return/cancel URLs.
- `PLATE_RECOGNIZER_API_TOKEN` is required only when the user actually scans a plate image.
- `ADMIN_PASSWORD` is separate from the Google login and is validated through a fixed-window rate limiter.

### 5.2 Create the database

This project uses EF Core migrations, not a hand-written SQL script. Apply the migrations from the repo root:

```powershell
dotnet ef database update --project src\GarageHub.Repository --startup-project src\GarageHub.Web
```

If `dotnet-ef` is not installed yet, use:

```powershell
dotnet tool install --global dotnet-ef
```

### 5.3 Configure Google OAuth redirect URI

In Google Cloud Console, add the following Authorized redirect URI to your OAuth web client:

```text
https://localhost:7100/signin-google
```

This matches the app's HTTPS launch profile. The app also includes an HTTP launch profile at `http://localhost:5133`, but the redirect URI for Google must match the HTTPS one above.

### 5.4 Run the app

```powershell
dotnet run --project src\GarageHub.Web --launch-profile https
```

Then open the browser to the HTTPS endpoint shown in the launch profile. If the browser warns about development certificates, trust the local HTTPS cert and continue.

## 6. User flows

### 6.1 Customer sign-in and first-time setup

1. Open the app.
2. Select Login with Google.
3. Complete Google authentication with a verified email account.
4. If the provided Google account is already known, the app signs the user in and redirects them to booking creation.
5. If the account is new, the app redirects to the complete profile view and asks for a full name.
6. After the profile is saved, the app creates the customer record and signs the user in to the main app cookie.

The customer session stores:

- user ID
- full name
- email
- role (`Customer`)

### 6.2 Manage vehicles

From the top navigation, the logged-in customer can open My vehicles.

Actions:

- Add vehicle: plate number, brand, model
- Edit vehicle: update details
- Delete vehicle: only allowed if the vehicle does not have related booking/reminder data

Vehicle rules enforced by the code:

- plate numbers must be unique across the system
- a vehicle belongs to the signed-in customer
- a vehicle cannot be deleted if it is referenced by a booking or reminder

### 6.3 Scan a license plate from the booking page

The booking form includes a Scan license plate from image button. When the user selects an image, the app sends it to `/api/ocr/scan-plate`.

Flow:

- the image is uploaded to the OCR API
- the result is normalized and compared against the customer's saved vehicles
- if a matching plate is found, the related vehicle is selected automatically
- if the plate does not match any vehicle in the list, the user is informed and must still select the correct vehicle manually

The OCR service calls the Plate Recognizer Snapshot Cloud API using the `PLATE_RECOGNIZER_API_TOKEN` secret. If the token is missing, the feature fails when a user attempts to scan.

### 6.4 Create a booking

From the authenticated home page, choose Add booking.

Booking form fields:

- Vehicle
- Garage
- Date
- Time slot

The time slots are fixed and shared across garages:

- 08:00–10:00
- 10:00–12:00
- 13:00–15:00
- 15:00–17:00

Rules enforced by the app:

- selected date and slot must be in the future
- time is handled in Vietnam local time (UTC+7)
- each garage has a `CapacityPerSlot` limit
- bookings count toward capacity when they are in `Pending`, `Confirmed`, or `InProgress` states
- canceled bookings do not count toward capacity
- booking holds are limited to 15 minutes while the payment is pending

When the user submits the booking form, the app creates a pending booking and immediately creates a PayOS payment link for the deposit.

### 6.5 PayOS deposit flow

The booking deposit amount is `10,000 VND`.

The payment flow is:

1. Customer submits booking form
2. App creates a `Pending` booking and a `Pending` payment row
3. App generates a PayOS checkout link
4. Customer pays on the PayOS page
5. PayOS returns to `/Payment/Return` or `/Payment/Cancel`
6. The app checks the payment status and updates the booking/payment state

Important behavior in the code:

- if payment link creation fails, the app cancels the pending payment and releases the slot
- expired pending bookings release their slot automatically
- if the payment status is still pending during return, the app performs a direct PayOS status check before deciding how to present the result
- the app never trusts the query string alone to authorize a success/cancel result

From the home page, the current customer UI displays bookings with:

- scheduled date/time
- plate number
- garage name
- status
- deposit amount

## 7. Admin and garage management

The Admin Functions button is visible from the home page even when not signed in.

Step-by-step:

1. Click Admin Functions
2. Enter the configured `ADMIN_PASSWORD`
3. The app creates a separate admin cookie and redirects to the garage page
4. The password route uses a fixed-window rate limiter (5 attempts per IP in 5 minutes)

Garage management is available to:

- authenticated `GarageOwner` users
- authenticated `Admin` users
- password-authenticated admin sessions

Garage page actions:

- Add garage
- Edit garage
- Delete garage

Garage validation rules:

- Name, Address, and Phone are required
- Owner is optional but must be an existing user if selected
- Capacity per slot must be at least 1
- Average rating is shown but not editable in the form

A garage cannot be deleted if it has related booking records.

## 8. Business rules and constraints

These rules are enforced in the project code and are important for end users and testers:

- Every booking is tied to one saved vehicle and one garage.
- Every service slot is two hours long.
- Booking slots are fixed by time rather than custom start/end times.
- Each garage has a maximum number of bookings per slot based on `CapacityPerSlot`.
- Capacity is counted for slots with statuses `Pending`, `Confirmed`, and `InProgress`.
- `Canceled` bookings do not count toward capacity.
- A `Pending` booking holds a slot for 15 minutes before expiring.
- A pending booking that expires is cleaned up by the background job.
- Plate numbers must be unique across all vehicles in the system.
- Time values are stored in Vietnam time (UTC+7) and all internal time checks use the shared helper rather than raw `DateTime.Now` or `DateTime.UtcNow` calls.

## 9. Background jobs and system behavior

The app registers a hosted background job named `BookingExpirationJob`.

Purpose:

- expire stale `Pending` bookings when their hold time is over
- release capacity when payment is not completed in time
- clean up related pending payment state when expired

This helps ensure a garage slot does not remain blocked forever if a customer never completes payment.

## 10. API endpoints and integration points

Important routes in the web app:

- `GET /Account/Login` — customer login page
- `GET /Account/GoogleLogin` — starts Google auth
- `GET /Account/ExternalLoginCallback` — handles Google callback
- `POST /Account/Logout` — sign out customer
- `GET /Home/Index` — landing page and booking summary
- `GET /Bookings/Create` — booking page
- `POST /Bookings/Create` — create pending booking and redirect to PayOS
- `GET /Payment/Return` — result page after successful PayOS redirect
- `GET /Payment/Cancel` — result page after cancellation
- `GET /Garages/Index` — garage management page
- `GET /Admin/Login` — admin password page
- `POST /api/ocr/scan-plate` — upload an image and receive recognized plate data

## 11. Known limitations in the current codebase

The current app has some intentional gaps:

- customer-side booking cancellation is not implemented in the UI
- booking edits are not exposed
- garage owners/admin role assignment is not available through a user-facing screen
- review creation and maintenance reminder flows are not implemented in the web UI
- OCR is integrated but depends on the external Plate Recognizer service and a valid API token

## 12. Troubleshooting

### Google login fails

- Verify the Google OAuth client ID and secret are set in user-secrets
- Check that the redirect URI exactly matches `https://localhost:7100/signin-google`
- Confirm that the Google account has a verified email address

### No garage or no vehicle appears

- Add a vehicle from My vehicles
- Make sure the database has been created and migrations applied
- As admin, create a garage if the list is empty

### Booking page shows errors

- Ensure the slot is a future date/time
- Check that the selected garage still has capacity
- Confirm the selected date/time slot matches one of the predefined fixed slots

### PayOS payment fails or redirects incorrectly

- Check `APP_PUBLIC_BASE_URL`
- Confirm PayOS client key, API key, and checksum key are set in user-secrets
- Ensure the PayOS callback/return/cancel URLs are reachable from the public internet when testing live payments

### OCR scan does not work

- Confirm `PLATE_RECOGNIZER_API_TOKEN` exists
- Use a clear image of the license plate
- Make sure the recognized plate matches one of the customer's saved vehicles

### Admin access is unavailable

- Set `ADMIN_PASSWORD` in user-secrets
- Restart the app after adding or changing the password
- Check that the admin password session has not expired

## 13. Development notes

This app follows a three-layer architecture:

- Web layer: controllers and Razor pages
- Service layer: validation, business logic, payment orchestration, OCR orchestration
- Repository layer: EF Core DbContext and data access

Important technical conventions in the project:

- `Users` are stored in the app database; no ASP.NET Identity tables are used
- time values use Vietnam local time with a shared helper instead of raw local clocks
- external credentials are kept in user-secrets, not in source-controlled files
- database operations are transactional where needed for booking capacity checks and pending-payment creation

## 14. Summary

GarageHub is a focused automotive service scheduling web app with a clear customer flow:

- sign in with Google
- add vehicles
- choose a fixed garage time slot
- pay the deposit through PayOS
- track the booking status from the home menu

The admin side adds garage setup and management behind a password-protected gate. The main remaining gaps are user-facing review/reminder management and more advanced role administration, but the core booking flow is wired up and operational in the current codebase.
