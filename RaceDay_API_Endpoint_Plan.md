# RaceDay API Endpoint Plan — Part 1

**Base URL:** `/api`

**Authentication:** JWT bearer authentication. Protected endpoints require `Authorization: Bearer <token>`.

**Content type:** `application/json` for requests and responses unless otherwise stated.

## Standard response conventions

- `200 OK` — successful read/update
- `201 Created` — successful creation
- `204 No Content` — successful deletion/cancellation where no response body is required
- `400 Bad Request` — validation error, malformed input, invalid state transition
- `401 Unauthorized` — missing or invalid JWT
- `403 Forbidden` — authenticated user does not have the required role or ownership
- `404 Not Found` — requested resource does not exist
- `409 Conflict` — duplicate email/registration/result or another database uniqueness conflict
- `502 Bad Gateway` — external weather service failure

## Endpoint plan

| HTTP Method | Route | Description / Purpose | Role Required | Request Body | Expected Response / Failure Cases |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Create a participant account | Public | `{ "firstName":"Sipho", "lastName":"Dlamini", "email":"sipho@example.com", "password":"Password123!", "phone":"0710000003" }` | **201:** `{ "userId":3, "message":"Registration successful" }` **400:** validation/password error; **409:** email already exists |
| POST | `/api/auth/login` | Authenticate a user and issue JWT | Public | `{ "email":"sipho@example.com", "password":"Password123!" }` | **200:** `{ "token":"<jwt>", "expiresIn":3600 }` **400:** missing fields; **401:** invalid credentials |
| GET | `/api/users/me` | Return the authenticated user's profile | Logged in | None | **200:** user profile; **401:** missing/invalid token |
| PUT | `/api/users/me` | Update the authenticated user's profile | Logged in | `{ "firstName":"Sipho", "lastName":"Dlamini", "phone":"0720000003" }` | **200:** updated profile; **400:** invalid data; **401:** unauthenticated |
| GET | `/api/events` | List and filter available events | Public | Query parameters, e.g. `?status=Open&location=Gqeberha` | **200:** event array; **400:** invalid filter values |
| GET | `/api/events/{eventId}` | Get one event and its basic details | Public | None | **200:** event object; **404:** event not found |
| POST | `/api/events` | Create a new race/event | Organiser | `{ "name":"Gqeberha Coastal Run", "eventDate":"2026-10-18", "location":"Gqeberha", "distanceKm":10, "description":"Coastal road run", "status":"Open" }` | **201:** created event; **400:** validation error; **401:** unauthenticated; **403:** not organiser |
| PUT | `/api/events/{eventId}` | Update an event | Organiser | Event fields to change, e.g. `{ "name":"Gqeberha Coastal Run", "status":"Open" }` | **200:** updated event; **400:** invalid data; **403:** not owner/organiser; **404:** event not found |
| DELETE | `/api/events/{eventId}` | Cancel/remove an event | Organiser | None | **204:** event cancelled/deleted; **403:** forbidden; **404:** event not found; **409:** event cannot be removed because of dependent records |
| GET | `/api/events/{eventId}/categories` | List categories for an event | Public | None | **200:** category array; **404:** event not found |
| POST | `/api/events/{eventId}/categories` | Create an entry category for an event | Organiser | `{ "name":"10 km Open", "maxParticipants":500, "entryFee":150.00 }` | **201:** category created; **400:** invalid capacity/fee; **403:** forbidden; **404:** event not found; **409:** duplicate category name |
| PUT | `/api/categories/{categoryId}` | Update a category | Organiser | `{ "name":"10 km Open", "maxParticipants":550, "entryFee":160.00 }` | **200:** updated category; **400:** invalid data; **403:** forbidden; **404:** category not found; **409:** duplicate name |
| DELETE | `/api/categories/{categoryId}` | Delete a category | Organiser | None | **204:** deleted; **403:** forbidden; **404:** category not found; **409:** category has enrolments |
| POST | `/api/categories/{categoryId}/enrolments` | Register current participant for a category | Participant | `{ "raceNumber":105 }` | **201:** `{ "enrolmentId":5, "status":"Registered" }`; **400:** invalid race number; **401:** unauthenticated; **403:** not participant; **404:** category not found; **409:** already registered/full category |
| GET | `/api/users/me/enrolments` | View the current participant's entries | Participant | None | **200:** enrolment array; **401:** unauthenticated; **403:** wrong role |
| GET | `/api/events/{eventId}/enrolments` | View entries for an event | Organiser | None | **200:** enrolment array; **403:** forbidden; **404:** event not found |
| PUT | `/api/enrolments/{enrolmentId}` | Update an enrolment status/details | Participant/Organiser | `{ "status":"Confirmed", "raceNumber":105 }` | **200:** updated enrolment; **400:** invalid status; **401:** unauthenticated; **403:** not owner/organiser; **404:** enrolment not found; **409:** race number already used |
| DELETE | `/api/enrolments/{enrolmentId}` | Cancel a participant's enrolment | Participant/Organiser | None | **204:** cancelled; **401:** unauthenticated; **403:** not owner/organiser; **404:** enrolment not found |
| POST | `/api/events/{eventId}/results` | Record a result for an event enrolment | Organiser | `{ "enrolmentId":1, "finishTime":"00:48:30", "position":12, "paceMinPerKm":4.85, "resultStatus":"Finished" }` | **201:** result created; **400:** invalid result; **403:** forbidden; **404:** event/enrolment not found; **409:** enrolment already has a result |
| GET | `/api/events/{eventId}/results` | List results for an event | Public | Optional sorting/filter query parameters | **200:** result array; **404:** event not found |
| GET | `/api/users/me/results` | View the current participant's result history | Participant | None | **200:** result history; **401:** unauthenticated; **403:** wrong role |
| GET | `/api/results/{resultId}` | View one result | Public | None | **200:** result object; **404:** result not found |
| PUT | `/api/results/{resultId}` | Correct an existing result | Organiser | `{ "finishTime":"00:47:55", "position":10, "paceMinPerKm":4.79, "resultStatus":"Finished" }` | **200:** corrected result; **400:** invalid values; **403:** forbidden; **404:** result not found |
| DELETE | `/api/results/{resultId}` | Remove an incorrect result | Organiser | None | **204:** deleted; **403:** forbidden; **404:** result not found |
| GET | `/api/events/{eventId}/route` | View the route assigned to an event | Public | None | **200:** route object; **404:** event/route not found |
| PUT | `/api/events/{eventId}/route` | Add or update the event route | Organiser | `{ "startPoint":"Kings Beach", "finishPoint":"Kings Beach", "routeUrl":"https://example.com/routes/coastal-run", "elevationGainM":85, "notes":"Coastal out-and-back route." }` | **200:** route saved; **400:** invalid route data; **403:** forbidden; **404:** event not found; **409:** conflicting route record |
| GET | `/api/events/{eventId}/weather` | Return current/forecast weather using an external weather service | Public | Optional query parameters such as forecast date | **200:** weather response; **400:** invalid location/date; **404:** event not found; **502:** external weather provider unavailable |

## Ownership and validation rules

1. Only users with `Role = Organiser` may create or manage events, categories, routes and results.
2. Participants may create and manage their own enrolments and view their own result history.
3. Organisers may manage records belonging to events they organise.
4. Email addresses must be unique.
5. A participant cannot enrol twice in the same category.
6. A category cannot exceed `MaxParticipants`.
7. A race number, when supplied, must be positive and unique.
8. An enrolment can have at most one result, matching the database `UNIQUE` constraint on `Results.EnrolmentID`.
9. External weather-service errors are returned as `502 Bad Gateway` rather than exposing provider errors directly.
