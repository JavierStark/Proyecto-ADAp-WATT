# CUDECA WATT - Frontend Application

The frontend application for CUDECA WATT, a comprehensive donation and event management platform built with Angular 19.

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.15.

## 🛠️ Technologies

- **Angular**: 19.2.0
- **TypeScript**: 5.7.2
- **Tailwind CSS**: 3.4.18
- **RxJS**: 7.8.0
- **Supabase JS**: 2.87.1

## 📋 Features

- **User Authentication**: Login and registration with Supabase Auth
- **Event Management**: Browse and search events, view event details
- **Ticket Purchase**: Buy tickets with multiple payment methods
- **Donation System**: Make one-time or recurring donations
- **Partner Membership**: Subscribe to partner programs
- **Corporate Profiles**: Manage corporate donor profiles
- **User Dashboard**: View donation history, purchased tickets, and profile
- **QR Validation**: Validate event tickets via QR codes
- **Responsive Design**: Mobile-first design with Tailwind CSS

## 🚀 Prerequisites

- **Node.js**: v18 or higher
- **npm**: Comes with Node.js
- **Angular CLI**: v19.2.15 (install globally with `npm install -g @angular/cli`)

## 🏃 Getting Started

### Installation

1. **Install dependencies:**

```bash
npm install
```

2. **Configure API endpoint:**

Update the API URL in service files (`src/app/services/*.service.ts`) to point to your backend:

```typescript
private apiUrl = 'http://localhost:5000/'; // Development
// private apiUrl = 'https://your-backend.azurewebsites.net/'; // Production
```

### Development server

To start a local development server, run:

```bash
npm start
# or
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## 🔨 Building

To build the project for production:

```bash
npm run build
# or
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

For development build with watch mode:

```bash
npm run watch
# or
ng build --watch --configuration development
```

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## 📁 Project Structure

```
src/
├── app/
│   ├── home/              # Landing page
│   ├── login/             # Login page
│   ├── sign-up/           # Registration page
│   ├── cuenta/            # User account dashboard
│   ├── donation/          # Donation page
│   ├── eventos/           # Events list
│   ├── evento-detalles/   # Event details
│   ├── compra-entradas/   # Ticket purchase
│   ├── compra-finalizada/ # Purchase confirmation
│   ├── pagos/             # Payment processing
│   ├── hazte-socio/       # Partner subscription
│   ├── qr-validate/       # QR code validation
│   ├── services/          # API services
│   ├── guards/            # Route guards
│   ├── interceptors/      # HTTP interceptors
│   └── layouts/           # Layout components
├── assets/                # Static assets
└── styles.css             # Global styles
```

## 🔑 Key Components

- **Authentication Guard**: Protects routes requiring login
- **Public Guard**: Redirects authenticated users away from login/signup
- **Auth Service**: Handles authentication with Supabase
- **API Services**: Communicate with backend REST API
- **HTTP Interceptors**: Handle authentication tokens

## 🎨 Styling

This project uses **Tailwind CSS** for styling. The configuration can be found in `tailwind.config.js`.

To customize Tailwind:

1. Edit `tailwind.config.js` for theme customization
2. Global styles are in `src/styles.css`
3. Component-specific styles use Tailwind utility classes

## 🌐 API Integration

The application integrates with the CUDECA WATT backend API. Services are located in `src/app/services/`:

- `auth.service.ts` - Authentication and user management
- `compra.service.ts` - Ticket purchase operations
- `partner.service.ts` - Partner membership
- `partner-api.service.ts` - Partner API communication
- `company.service.ts` - Corporate profile management

## 🔒 Route Guards

- **authGuard**: Ensures user is authenticated before accessing protected routes
- **publicGuard**: Prevents authenticated users from accessing public-only routes (login/signup)

## 📱 Responsive Design

The application is fully responsive and works on:
- Desktop (1024px and up)
- Tablet (768px - 1023px)
- Mobile (up to 767px)

## 🐛 Troubleshooting

**Problem**: API calls fail with CORS errors
- **Solution**: Ensure backend CORS is configured to allow `http://localhost:4200`

**Problem**: Authentication not working
- **Solution**: Check Supabase configuration in services and ensure backend is running

**Problem**: Styles not loading
- **Solution**: Run `npm install` and restart dev server

## 📚 Additional Resources

- [Angular Documentation](https://angular.dev)
- [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Supabase JS Client](https://supabase.com/docs/reference/javascript)
- [RxJS Documentation](https://rxjs.dev)

## 🔗 Related

For the complete project documentation, see the main [README.md](../../README.md) in the root directory.
