# GarageHub

GarageHub is a garage booking and vehicle management web application built with ASP.NET Core MVC.

For the full user guide, see [USER_GUIDE.md](./USER_GUIDE.md).

## Overview

- Google sign-in for customers
- Vehicle management per customer
- Fixed-slot booking workflow for garage service appointments
- PayOS deposit flow
- Admin password gate for garage management
- OCR-based plate matching during booking

## Project structure

- src/GarageHub.Web — MVC web application
- src/GarageHub.Service — business logic
- src/GarageHub.Repository — EF Core data access and migrations
- src/GarageHub.Common — shared DTOs, constants, and helpers
- tests/GarageHub.Tests — test project
