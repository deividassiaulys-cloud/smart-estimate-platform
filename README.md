# Smart Estimate Management Platform

## About the Project
A web application for construction service providers and contractors that
enables fast, error-free preparation of construction estimates from a
defined catalog of materials, works, prices, and norms, with automatic
recalculation of totals.

## Problem
Estimates are currently prepared in Excel spreadsheets or in programs
installed on a single computer, which limits work to one location, makes
price updates manual, and carries a high risk of errors.

## Target User Groups
- **Manager** – the company owner / estimator, manages the catalog and estimates.
- **Viewer** – an employee (e.g. on a construction site), sees only the
  lists of works and materials, without prices.

## Core Functionality (MVP)
- Registration with company creation
- Material and work catalog with work compositions (material norms)
- Project and estimate creation with automatic recalculation
- Estimate status: Draft / Confirmed
- Role-based viewing
- Export to Excel

## Project Status
Requirements and design phase.

## Technologies
- **Backend:** ASP.NET Core Web API (.NET)
- **ORM / Data Access:** Entity Framework Core (Microting.EntityFrameworkCore.MySql provider)
- **Database:** MySQL (managed via MySQL Workbench)
- **Frontend:** React (Vite, TypeScript)
- **Authentication:** ASP.NET Core Identity (password hashing) + JWT

## Documentation
- docs/requirements.md – full project requirements specification
- docs/ux.md – user experience, user flows, and wireframes
- specs/ – Spec Kit feature specifications
