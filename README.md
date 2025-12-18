# 🎗️ CUDECA WATT - Donation & Events Management Platform

A full-stack web application designed to manage donations and events for CUDECA, a charitable organization. This platform enables users to make donations, register for events, purchase tickets, and manage their profiles, while providing administrators with comprehensive management tools.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Technologies Used](#technologies-used)
  - [Frontend](#frontend)
  - [Backend](#backend)
  - [Infrastructure & Tools](#infrastructure--tools)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [API Endpoints](#api-endpoints)
- [Database Models](#database-models)
- [Configuration](#configuration)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Development Team](#development-team)
- [Contributing](#contributing)
- [License](#license)

## 🌟 Overview

CUDECA WATT is a comprehensive platform built to facilitate charitable donations and event management. The application provides a seamless user experience for donors and event participants while offering robust administrative capabilities for organization staff.

## ✨ Features

### For Users

- 🔐 **Authentication & Authorization** - Secure user registration and login using Supabase Auth
- 💰 **Donation Management** - Make donations (authenticated or anonymous) and view donation history
- 📜 **Tax Certificates** - Generate annual donation certificates in PDF format for tax purposes
- 🎫 **Event Registration** - Browse, search, and register for upcoming events
- 🎟️ **Ticket Purchase** - Purchase and manage event tickets (authenticated or anonymous)
- 🏷️ **Discount Codes** - Apply promotional discount codes to ticket purchases
- 📧 **Email Delivery** - Automatic email delivery of tickets and certificates
- 👤 **Profile Management** - Update personal information and view activity
- 📊 **Dashboard** - Personalized donation summary and statistics
- 🤝 **Partner Membership** - Subscribe to partner programs (monthly, quarterly, or annual plans)
- 🏢 **Corporate Profiles** - Create and manage corporate donor profiles
- 📱 **QR Code Tickets** - Digital tickets with QR codes for event entry validation

### For Administrators

- 🛡️ **Admin Panel** - Dedicated administrative interface
- 📝 **Event Management** - Create, update, and delete events with image uploads
- 🎟️ **Ticket Type Management** - Configure multiple ticket types per event (General, VIP, etc.)
- 🔍 **QR Validation** - Validate ticket QR codes at event entrances
- 👥 **User Management** - View and manage registered users
- 💳 **Payment Processing** - Monitor donations and ticket sales
- 📈 **Analytics** - Track donations and event participation

## 🏗️ Architecture

The application follows a modern **client-server architecture** with clear separation of concerns:

- **Frontend**: Single Page Application (SPA) built with Angular 19
- **Backend**: RESTful API built with ASP.NET Core 9.0 (Minimal API)
- **Database**: Supabase (PostgreSQL)
- **Authentication**: Supabase Auth with JWT tokens
- **Deployment**: Docker containerization ready

## 🛠️ Technologies Used

### Frontend

| Technology         | Version | Purpose                                 |
| ------------------ | ------- | --------------------------------------- |
| **Angular**        | 19.2.0  | Frontend framework for building the SPA |
| **TypeScript**     | 5.7.2   | Type-safe programming language          |
| **RxJS**           | 7.8.0   | Reactive programming with observables   |
| **Tailwind CSS**   | 3.4.18  | Utility-first CSS framework for styling |
| **Angular Router** | 19.2.0  | Client-side routing and navigation      |
| **Angular Forms**  | 19.2.0  | Form handling and validation            |

**Key Frontend Features:**

- Responsive design with Tailwind CSS
- Route guards for authentication
- Service-based architecture for API communication
- Component-based UI structure
- Lazy loading for optimized performance

### Backend

| Technology          | Version         | Purpose                                      |
| ------------------- | --------------- | -------------------------------------------- |
| **ASP.NET Core**    | 9.0             | Backend framework using Minimal API approach |
| **C#**              | 12.0 (.NET 9.0) | Primary backend language                     |
| **Supabase Client** | 1.1.1           | Database and authentication client           |
| **Swagger/OpenAPI** | 7.2.0           | API documentation and testing                |
| **Scalar**          | 2.11.5          | Modern API documentation UI                  |
| **QuestPDF**        | 2025.7.4        | PDF generation for certificates              |
| **QRCoder**         | 1.7.0           | QR code generation for tickets               |
| **Stripe.NET**      | 50.0.0          | Payment processing integration               |
| **RestSharp**       | 113.0.0         | HTTP client for external API calls           |

**Key Backend Features:**

- RESTful API architecture with Minimal API pattern
- JWT-based authentication via Supabase
- Custom authentication filters (`SupabaseAuthFilter`, `AdminAuthFilter`, `OptionalAuthFilter`)
- CORS configuration for secure cross-origin requests
- Strongly-typed models with Postgrest attributes
- User Secrets for secure configuration management
- Docker support for containerized deployment
- Email service integration with Mailgun
- Payment processing with Stripe (configurable with simulated mode for development)
- PDF certificate generation with QuestPDF
- QR code generation and validation for tickets
- Support for authenticated and anonymous transactions

### Infrastructure & Tools

| Technology       | Purpose                                           |
| ---------------- | ------------------------------------------------- |
| **Supabase**     | Backend-as-a-Service (PostgreSQL database + Auth) |
| **Docker**       | Containerization and deployment                   |
| **Git**          | Version control                                   |
| **Swagger UI**   | Interactive API documentation                     |
| **Scalar**       | Modern alternative API documentation              |
| **Mailgun**      | Email delivery service for tickets & certificates |
| **Stripe**       | Payment processing platform                       |
| **Azure**        | Cloud hosting and deployment                      |
| **User Secrets** | Secure configuration management                   |

## 📁 Project Structure

```
Proyecto-ADAp-WATT/
├── Backend/
│   ├── Backend.sln                    # Solution file
│   ├── docker-compose.yml             # Docker composition
│   └── Backend/
│       ├── Program.cs                 # Application entry point
│       ├── Backend.csproj             # Project configuration
│       ├── Dockerfile                 # Docker configuration
│       ├── WebApplicationExtensions.cs # Endpoint registration
│       │
│       ├── Models/                    # Database models
│       │   ├── Usuario.cs
│       │   ├── Cliente.cs
│       │   ├── Evento.cs
│       │   ├── Donacion.cs
│       │   ├── Entrada.cs
│       │   ├── EntradaEvento.cs
│       │   ├── Pago.cs
│       │   ├── Admin.cs
│       │   ├── Socio.cs
│       │   ├── PeriodoSocio.cs
│       │   ├── Corporativo.cs
│       │   ├── ValeDescuento.cs
│       │   └── UsuarioNoRegistrado.cs
│       │
│       ├── Endpoints/                 # API endpoint definitions
│       │   ├── AdminEndpoints.cs
│       │   ├── Donations.cs
│       │   ├── Events.cs
│       │   ├── Tickets.cs
│       │   ├── Profile.cs
│       │   ├── Auth.cs
│       │   ├── Partner.cs
│       │   ├── Corporate.cs
│       │   ├── DevTools.cs
│       │   └── Test.cs
│       │
│       ├── Filters/                   # Authentication filters
│       │   ├── SupabaseAuthFilter.cs
│       │   ├── AdminAuthFilter.cs
│       │   ├── OptionalAuthFilter.cs
│       │   └── SwaggerEmptyStringDefaultFilter.cs
│       │
│       ├── Services/                  # External service integrations
│       │   ├── IEmailService.cs
│       │   ├── MailGunService.cs
│       │   ├── IPaymentService.cs
│       │   ├── StripePaymentService.cs
│       │   └── SimulatedPaymentService.cs
│       │
│       ├── ResponseDtos.cs           # API response DTOs
│       ├── Settings.cs               # Configuration settings
│       └── appsettings.json          # Application configuration
│
├── Frontend/
│   └── cudecaApp/
│       ├── angular.json              # Angular configuration
│       ├── package.json              # NPM dependencies
│       ├── tailwind.config.js        # Tailwind configuration
│       │
│       └── src/
│           ├── app/
│           │   ├── home/             # Home page component
│           │   ├── login/            # Login component
│           │   ├── sign-up/          # Registration component
│           │   ├── cuenta/           # User account component
│           │   ├── donation/         # Donation component
│           │   ├── eventos/          # Events list component
│           │   ├── evento-detalles/  # Event details component
│           │   ├── compra-entradas/  # Ticket purchase component
│           │   ├── compra-finalizada/ # Purchase confirmation
│           │   ├── pagos/            # Payment processing
│           │   ├── hazte-socio/      # Partner subscription
│           │   ├── qr-validate/      # QR code validation
│           │   ├── help-modal/       # Help modal component
│           │   ├── layouts/          # Layout components
│           │   ├── services/         # API services
│           │   │   ├── auth.service.ts
│           │   │   ├── compra.service.ts
│           │   │   ├── partner.service.ts
│           │   │   ├── partner-api.service.ts
│           │   │   └── company.service.ts
│           │   ├── guards/           # Route guards
│           │   │   ├── auth.guard.ts
│           │   │   └── public.guard.ts
│           │   ├── interceptors/     # HTTP interceptors
│           │   └── app.routes.ts     # Application routes
│           │
│           ├── assets/               # Static assets
│           │   └── images/
│           ├── styles.css            # Global styles
│           └── index.html            # Main HTML file
│
├── DiagramClase.xmi                  # UML class diagram
├── DiagramSequence.xmi               # UML sequence diagram
├── EntityRelationship.xmi            # Entity relationship diagram
└── UseCase.xmi                       # Use case diagram
```

## 🚀 Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** (v18 or higher) and **npm**
- **.NET 9.0 SDK**
- **Docker** (optional, for containerized deployment)
- **Supabase Account** (for database and authentication)

### Backend Setup

1. **Navigate to the Backend directory:**

   ```bash
   cd Backend/Backend
   ```

2. **Configure required services using User Secrets:**

   ```bash
   # Supabase configuration (required)
   dotnet user-secrets set "Supabase:Url" "https://your-project.supabase.co"
   dotnet user-secrets set "Supabase:Key" "your-anon-key"
   
   # Mailgun configuration (required for email delivery)
   dotnet user-secrets set "MailGun:ApiKey" "your-mailgun-api-key"
   dotnet user-secrets set "MailGun:Domain" "your-domain.mailgun.org"
   
   # Stripe configuration (optional - uses simulated payment by default)
   dotnet user-secrets set "Stripe:SecretKey" "your-stripe-secret-key"
   ```

3. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

4. **Run the application:**

   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001` (or `http://localhost:5000`)

5. **Verify the API is running:**
   Navigate to `https://localhost:5001/` - you should see "CUDECA API"
   Test Supabase connection: `https://localhost:5001/test/supabase`

6. **Access API documentation:**
   - Swagger UI: `https://localhost:5001/swagger`
   - Scalar UI (modern alternative): `https://localhost:5001/scalar/v1`

### Frontend Setup

1. **Navigate to the Frontend directory:**

   ```bash
   cd Frontend/cudecaApp
   ```

2. **Install dependencies:**

   ```bash
   npm install
   ```

3. **Configure environment (if needed):**
   Update the API base URL in your services to point to the backend.

4. **Start the development server:**

   ```bash
   npm start
   ```

   The application will be available at `http://localhost:4200`

### Docker Setup (Optional)

To run the backend using Docker:

```bash
cd Backend
docker-compose up --build
```

## 🔌 API Endpoints

### Authentication

- `POST /auth/signup` - User registration
- `POST /auth/signin` - User login
- `POST /auth/signout` - User logout (requires authentication)

### User Profile & Status

- `GET /users/me` - Get current user profile (requires authentication)
- `PUT /users/me` - Update user profile (requires authentication)
- `GET /users/me/is-admin` - Check if user is admin (requires authentication)
- `GET /users/me/is-partner` - Check if user is partner (requires authentication)
- `GET /users/me/is-corporate` - Check if user is corporate (requires authentication)

### User Resources

- `GET /users/me/tickets` - Get user's purchased tickets (requires authentication)
- `GET /users/me/tickets/{ticketId}` - Get specific ticket details (requires authentication)
- `GET /users/me/donations` - Get user's donation history (requires authentication)
- `GET /users/me/donations/summary` - Get donation summary (requires authentication)

### Donations

- `POST /donations` - Create a donation (authenticated or anonymous)
- `POST /donations/certificate/annual` - Generate annual donation certificate PDF (requires authentication)

### Events

- `GET /events` - List all visible events (supports search with `query` parameter)
- `GET /events/{eventId}` - Get event details

### Tickets

- `GET /tickets/type/event/{eventId}` - Get ticket types for specific event
- `POST /tickets/purchase` - Purchase event tickets (authenticated or anonymous)
- `GET /tickets/validate` - Validate ticket QR code

### Payments & Discounts

- `GET /payments/methods` - Get available payment methods
- `POST /discounts/validate` - Validate discount code

### Partners

- `POST /partners/subscribe` - Subscribe as partner (requires authentication)
- `GET /partners/data` - Get partner membership data (requires authentication)

### Corporate

- `POST /company` - Create or update corporate profile (requires authentication)
- `GET /company` - Get corporate profile data (requires authentication)

### Admin - Events Management

- `GET /admin/events` - List all events including hidden ones (requires admin)
- `POST /admin/events` - Create new event with image upload (requires admin)
- `PUT /admin/events/{eventId}` - Update event (requires admin)
- `DELETE /admin/events/{eventId}` - Delete event (requires admin)

### Development (Dev environment only)

- `GET /dev/dtos` - Get all DTO structures for API integration

## 📊 Database Models

### Core Models

- **Usuario** - User information (ID, email, DNI, name, phone, address)
- **Cliente** - Client/donor profile linked to Usuario
- **UsuarioNoRegistrado** - Anonymous user data for non-authenticated transactions
- **Admin** - Administrator accounts with elevated privileges
- **Evento** - Event details (name, description, date, location, capacity, image)
- **Entrada** - Individual ticket instances with QR codes and purchase information
- **EntradaEvento** - Ticket types for events (General, VIP, etc.) with pricing and stock
- **Donacion** - Donation records with amount, date, and donor information
- **Pago** - Payment information for donations and tickets (method, amount, status)
- **Socio** - Partner membership records linked to Cliente
- **PeriodoSocio** - Partner membership periods with dates and fees
- **Corporativo** - Corporate donor profiles with company information
- **ValeDescuento** - Discount/voucher codes with expiration and usage limits

## ⚙️ Configuration

### Backend Configuration

The backend requires several configuration settings that should be stored securely using .NET User Secrets in development:

#### Required Configuration

1. **Supabase** - Database and authentication
   - `Supabase:Url` - Your Supabase project URL
   - `Supabase:Key` - Your Supabase anon/public key

2. **Mailgun** - Email delivery service
   - `MailGun:ApiKey` - Your Mailgun API key
   - `MailGun:Domain` - Your Mailgun domain

#### Optional Configuration

3. **Stripe** - Payment processing (defaults to simulated payment service)
   - `Stripe:SecretKey` - Your Stripe secret key

### Frontend Configuration

The frontend connects to the backend API. Update the API URL in service files:

- Located in: `Frontend/cudecaApp/src/app/services/*.service.ts`
- Default development: `http://localhost:5000` or `https://localhost:5001`
- Production: Configure to point to your deployed backend URL

### CORS Configuration

The backend is configured to accept requests from:
- `http://localhost:4200` (development)
- `https://cudeca-watt.es` (production)
- `https://www.cudeca-watt.es` (production)

Update `Program.cs` to add additional allowed origins if needed.

### Payment Processing

The application supports two payment modes:

1. **Simulated Mode** (default) - For development and testing
2. **Stripe Integration** - For production use

Configure in `Program.cs` by uncommenting the environment-based payment service selection.

## 🚀 Deployment

### Backend Deployment

The backend is containerized and can be deployed using Docker:

```bash
cd Backend
docker-compose up --build
```

The application is deployed on **Azure App Service**. Configure your production URL in environment variables.

### Frontend Deployment

Build the Angular application for production:

```bash
cd Frontend/cudecaApp
npm run build
```

The production build will be in `dist/` directory. Deploy to your preferred hosting service.

### Environment Variables for Production

For production deployment, configure environment variables instead of User Secrets:

- `Supabase__Url`
- `Supabase__Key`
- `MailGun__ApiKey`
- `MailGun__Domain`
- `Stripe__SecretKey` (optional)

## 👥 Development Team

### Frontend Team

<!-- Add frontend team members here -->

- **Team Member 1** - Role/Responsibilities
- **Team Member 2** - Role/Responsibilities
- **Team Member 3** - Role/Responsibilities

### Backend Team

<!-- Add backend team members here -->

- **Team Member 1** - Role/Responsibilities
- **Team Member 2** - Role/Responsibilities
- **Team Member 3** - Role/Responsibilities

---

## 🔧 Troubleshooting

### Backend Issues

**Problem**: "Supabase configuration is missing" error
- **Solution**: Ensure you've set up user secrets for Supabase:Url and Supabase:Key

**Problem**: "MailGun configuration is missing" error
- **Solution**: Configure Mailgun credentials using user secrets

**Problem**: CORS errors when calling API from frontend
- **Solution**: Check that your frontend URL is listed in the CORS policy in `Program.cs`

**Problem**: Payment processing fails
- **Solution**: The app uses simulated payments by default. For Stripe integration, configure Stripe secret key

### Frontend Issues

**Problem**: API calls fail with 404 or connection errors
- **Solution**: Verify the backend is running and the API URL in services matches your backend URL

**Problem**: Authentication not working
- **Solution**: Check that Supabase credentials are correctly configured in both backend and frontend

### Database Issues

**Problem**: Database queries fail
- **Solution**: Verify Supabase connection and ensure database tables match the models

**Problem**: Missing tables or columns
- **Solution**: Review the database models in `Backend/Models/` and ensure your Supabase database schema matches

## 📚 Additional Resources

- **API Documentation**: Access Swagger UI at `/swagger` or Scalar UI at `/scalar/v1` when running the backend
- **Supabase Documentation**: [https://supabase.com/docs](https://supabase.com/docs)
- **Angular Documentation**: [https://angular.dev](https://angular.dev)
- **Tailwind CSS**: [https://tailwindcss.com/docs](https://tailwindcss.com/docs)

---

## 📄 License

This project is developed for CUDECA as part of the ADAp course. All rights reserved.

---

## 🤝 Contributing

This is an academic project. For any questions or suggestions, please contact the development team.

---

## 📧 Contact

For more information about CUDECA and their mission, visit their official website or contact the project maintainers.

---

**Made with ❤️ for CUDECA**
