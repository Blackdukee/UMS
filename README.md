# 📖 User Management System

## Comprehensive API Documentation and Process Flow

### Overview
This API provides endpoints for user authentication, profile management, role administration, notifications, and system health checks.  
Key features:
- Secure registration/login with JWT and API keys.
- Google login integration.
- Rate-limited password reset with OTP.
- Role-based access for Admin endpoints.
- Internal and inter-service communication via service keys.
- Notifications processing for various user actions.

### Authentication Endpoints

#### Register User
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/auth/register`
- **Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "SecurePass123!"
}
```
- **Response:**
```json
{
  "message": "Registration successful."
}
```
- **Process Flow (Front End):**
  1. User fills registration form.
  2. Front end sends payload with user details.
  3. Backend validates, hashes password, saves user record.
  4. Confirmation message returned.

#### Login
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/auth/login`
- **Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass123!"
}
```
- **Response:**
```json
{
  "accessToken": "<JWT token>",
  "refreshToken": "<refresh token>"
}
```
- **Process Flow:**
  1. Credentials validated and tokens generated.
  2. Front end stores JWT for subsequent requests.

#### Google Login
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/auth/google-login`
- **Request Body:**
```json
{
  "IdToken": "google_id_token_here"
}
```
- **Response:**
```json
{
  "accessToken": "<JWT token>",
  "refreshToken": "<refresh token>",
  "user": { "id": 1, "email": "john.doe@example.com", "role": "Student" }
}
```
- **Note:** Bypasses standard JWT validation using Google credentials.

#### Forgot Password & Reset Password
- **Forgot Password:**  
  - **Method:** POST  
  - **Endpoint:** `/api/v1/ums/auth/forgot-password`
  - **Request Body:**
  ```json
  {
    "email": "john.doe@example.com"
  }
  ```
  - **Response:**
  ```json
  {
    "message": "OTP has been sent to your email"
  }
  ```
- **Reset Password:**  
  - **Method:** POST  
  - **Endpoint:** `/api/v1/ums/auth/reset-password`
  - **Request Body:**
  ```json
  {
    "email": "john.doe@example.com",
    "otp": "123456",
    "newPassword": "NewSecurePass456!"
  }
  ```
  - **Response:**
  ```json
  {
    "message": "Password has been updated successfully"
  }
  ```
- **Process Flow:**  
  1. Front end calls forgot-password; OTP is sent via email.
  2. User enters received OTP along with a new password.
  3. Reset endpoint validates OTP and updates password.

#### Refresh Token
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/auth/refresh-token`
- **Request Body:**
```json
{
  "refreshToken": "<existing refresh token>"
}
```
- **Response:**
```json
{
  "accessToken": "<new JWT token>",
  "refreshToken": "<new refresh token>"
}
```
- **Process Flow:**  
  Front end uses the refresh token when the access token expires without forcing a full re-login.

#### Validate Token
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/auth/validate`
- **Request Body:**
```json
{
  "token": "<JWT token>"
}
```
- **Response:**
```json
{
  "valid": true,
  "user": {
    "id": 1,
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Student"
  }
}
```

### User Endpoints

#### Get Profile
- **Method:** GET  
- **Endpoint:** `/api/v1/ums/user/profile`
- **Response:**
```json
{
  "id": 1,
  "email": "john.doe@example.com",
  "role": "Student",
  // ...other profile data...
}
```
- **Usage:** Requires JWT in `Authorization` header (Bearer token).

#### Update Profile
- **Method:** PUT  
- **Endpoint:** `/api/v1/ums/user/profile`
- **Request Body:**
```json
{
  "email": "updated.email@example.com"
}
```
- **Response:**
```json
{
  "message": "Profile updated successfully."
}
```

#### Change Password
- **Method:** PUT  
- **Endpoint:** `/api/v1/ums/user/change-password`
- **Request Body:**
```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!"
}
```
- **Response:**
```json
{
  "message": "Password changed successfully."
}
```

#### Delete Account
- **Method:** DELETE  
- **Endpoint:** `/api/v1/ums/user/delete-account`
- **Response:**
```json
{
  "message": "Account deleted successfully."
}
```

#### Forgot Password (User context)
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/user/forgot-password`
- **Notes:** Similar OTP process as Auth endpoints; triggered when the user is logged in.

### Admin Endpoints
(Requires Admin role JWT)

#### Get All Users
- **Method:** GET  
- **Endpoint:** `/api/v1/ums/admin/users`
- **Response:**
```json
[
  {
    "id": 1,
    "email": "john.doe@example.com",
    "role": "Student"
  },
  { ... }
]
```

#### Set User Role
- **Method:** PUT  
- **Endpoint:** `/api/v1/ums/admin/set-role/{userId}`
- **Request Body:**
```json
{
  "role": "Instructor"
}
```
- **Response:**
```json
{
  "message": "User role updated successfully."
}
```

#### Delete User
- **Method:** DELETE  
- **Endpoint:** `/api/v1/ums/admin/delete-user/{userId}`
- **Response:**
```json
{
  "message": "User deleted successfully."
}
```

### Notifications Endpoints

#### Process Notifications
- **Method:** POST  
- **Endpoint:** `/api/v1/ums/notifications` or `/api/notifications`  
- **Request Body (Example - ENROLL_USER):**
```json
{
  "userId": 1,
  "action": "ENROLL_USER",
  "courseId": "COURSE123",
  "transactionId": "TX456"
}
```
- **Response:**
```json
{
  "success": true,
  "message": "Notification processed successfully"
}
```

#### Educator Notifications
- **Get User Notifications:**  
  - **Method:** GET  
  - **Endpoint:** `/api/v1/ums/notifications/user?includeRead=false`
  - **Response:**
  ```json
  {
    "notifications": [ /* array of notifications */ ],
    "unreadCount": 3
  }
  ```
- **Mark Notification as Read:**  
  - **Method:** PATCH  
  - **Endpoint:** `/api/v1/ums/notifications/{id}/read`
  - **Response:**
  ```json
  {
    "message": "Notification marked as read"
  }
  ```
- **Mark All as Read:**  
  - **Method:** POST  
  - **Endpoint:** `/api/v1/ums/notifications/mark-all-read`
  - **Response:**
  ```json
  {
    "message": "All notifications marked as read"
  }
  ```

### Health Endpoint
- **Method:** GET  
- **Endpoint:** `/api/v1/ums/health`
- **Response:**
```json
{
  "status": "API is running",
  "timestamp": "2025-06-11T12:34:56Z"
}
```

### Usage and Front-end Process Flow

1. **Initial Authentication:**  
   - New users register using the `/auth/register` endpoint.
   - Users log in via `/auth/login` (or `/auth/google-login` for Google credentials).
   - Front end stores the returned JWT and refresh token.

2. **Subsequent Requests:**  
   - For protected endpoints (User, Admin, Notifications), the JWT must be sent in the `Authorization` header.
   - API key (`X-Api-Key`) is required for public endpoints or inter-service calls.
   - In case of token expiry, the front end calls `/auth/refresh-token` to obtain new tokens.

3. **Password Reset Flow:**  
   - Both non-logged-in users and logged-in users can trigger OTP-based password reset.
   - The front end collects the OTP from email and calls `/auth/reset-password` accordingly.

4. **Admin and Notifications:**  
   - Admin users interact with endpoints under `/admin` to manage user roles and deletion.
   - Notifications (both general and educator-specific) are processed separately with dedicated endpoints. Ensure that for educator endpoints, appropriate role checks are enforced.

### Unobvious Considerations
- **API Key and Service Key Validation:**  
  Internal endpoints (such as token validation and notifications) may bypass standard JWT checks if a valid `X-Service-Key` is provided.
- **Rate Limiting:**  
  Certain endpoints (e.g., forgot-password) are rate-limited to prevent abuse.
- **Error Handling:**  
  Custom middlewares handle errors and log detailed information. Any backend error returns a structured error message.
- **JWT Configuration:**  
  JWT settings (secret, issuer, audience) are configured via environment variables and validated on startup.
- **FluentValidation:**  
  Request payloads are validated using FluentValidation to ensure integrity before processing.

---