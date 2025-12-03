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
- [Development Team](#development-team)
  - [Frontend Team](#frontend-team)
  - [Backend Team](#backend-team)
- [License](#license)

## 🌟 Overview

CUDECA WATT is a comprehensive platform built to facilitate charitable donations and event management. The application provides a seamless user experience for donors and event participants while offering robust administrative capabilities for organization staff.

## ✨ Features

### For Users

- 🔐 **Authentication & Authorization** - Secure user registration and login using Supabase Auth
- 💰 **Donation Management** - Make donations and view donation history
- 🎫 **Event Registration** - Browse, search, and register for upcoming events
- 🎟️ **Ticket Purchase** - Purchase and manage event tickets
- 👤 **Profile Management** - Update personal information and view activity
- 📊 **Dashboard** - Personalized donation summary and statistics

### For Administrators

- 🛡️ **Admin Panel** - Dedicated administrative interface
- 📝 **Event Management** - Create, update, and delete events
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

**Key Backend Features:**

- RESTful API architecture with Minimal API pattern
- JWT-based authentication via Supabase
- Custom authentication filters (`SupabaseAuthFilter`, `AdminAuthFilter`)
- CORS configuration for secure cross-origin requests
- Strongly-typed models with Postgrest attributes
- User Secrets for secure configuration management
- Docker support for containerized deployment

### Infrastructure & Tools

| Technology       | Purpose                                           |
| ---------------- | ------------------------------------------------- |
| **Supabase**     | Backend-as-a-Service (PostgreSQL database + Auth) |
| **Docker**       | Containerization and deployment                   |
| **Git**          | Version control                                   |
| **Swagger UI**   | Interactive API documentation                     |
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
│       │
│       ├── Models/                    # Database models
│       │   ├── Usuario.cs
│       │   ├── Evento.cs
│       │   ├── Donacion.cs
│       │   ├── Entrada.cs
│       │   ├── Pago.cs
│       │   └── Admin.cs
│       │
│       ├── Endpoints/                 # API endpoint definitions
│       │   ├── AdminEndpoints.cs
│       │   ├── Donations.cs
│       │   ├── Events.cs
│       │   ├── Tickets.cs
│       │   ├── Profile.cs
│       │   └── Auth.cs
│       │
│       ├── Filters/                   # Authentication filters
│       │   ├── SupabaseAuthFilter.cs
│       │   └── AdminAuthFilter.cs
│       │
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
│           │   ├── eventos/          # Events component
│           │   ├── layouts/          # Layout components
│           │   ├── services/         # API services
│           │   │   └── auth.service.ts
│           │   ├── guards/           # Route guards
│           │   └── app.routes.ts     # Application routes
│           │
│           ├── assets/               # Static assets
│           │   └── images/
│           ├── styles.css            # Global styles
│           └── index.html            # Main HTML file
│
└── Documentation/                    # UML diagrams
    ├── DiagramClase.xmi
    ├── DiagramSequence.xmi
    ├── EntityRelationship.xmi
    └── UseCase.xmi
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

2. **Configure Supabase credentials using User Secrets:**

   ```bash
   dotnet user-secrets set "Supabase:Url" "https://your-project.supabase.co"
   dotnet user-secrets set "Supabase:Key" "your-anon-key"
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

5. **Access Swagger documentation:**
   Navigate to `https://localhost:5001/swagger` to view and test the API endpoints.

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

- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/logout` - User logout

### Donations

- `GET /api/donations/my` - Get current user's donation history
- `GET /api/donations/summary` - Get donation summary and statistics
- `POST /api/donations` - Create a new donation

### Events

- `GET /api/events` - List all events (with optional search query)
- `GET /api/events/{id}` - Get event details
- `POST /api/events` - Create new event (Admin only)
- `PUT /api/events/{id}` - Update event (Admin only)
- `DELETE /api/events/{id}` - Delete event (Admin only)

### Tickets

- `GET /api/tickets/my` - Get user's purchased tickets
- `POST /api/tickets/purchase` - Purchase event ticket
- `GET /api/tickets/{id}` - Get ticket details

### Profile

- `GET /api/profile` - Get current user profile
- `PUT /api/profile` - Update user profile
- `DELETE /api/profile` - Delete user account

### Admin

- `GET /api/admin/users` - List all users (Admin only)
- `GET /api/admin/statistics` - Get platform statistics (Admin only)

## 📊 Database Models

### Core Models

- **Usuario** - User information (ID, email, DNI, name, phone)
- **Cliente** - Client/donor profile linked to Usuario
- **Admin** - Administrator accounts
- **Evento** - Event details (name, description, date, location, capacity)
- **Entrada** - Ticket types for events
- **EntradaEvento** - Junction table for ticket-event relationship
- **Donacion** - Donation records
- **Pago** - Payment information for donations and tickets

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
