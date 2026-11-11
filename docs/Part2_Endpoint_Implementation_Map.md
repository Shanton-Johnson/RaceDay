# Part 2 endpoint-to-controller map

The implementation follows the 24 endpoint routes planned in Part 1.

| Part 1 route | Controller | Action |
|---|---|---|
| POST `/api/auth/register` | AuthController | Register |
| POST `/api/auth/login` | AuthController | Login |
| POST `/api/auth/logout` | AuthController | Logout |
| GET `/api/profile` | ProfileController | GetProfile |
| PUT `/api/profile` | ProfileController | UpdateProfile |
| GET `/api/events` | EventsController | GetEvents |
| GET `/api/events/{eventId}` | EventsController | GetEventById |
| POST `/api/events` | EventsController | CreateEvent |
| PUT `/api/events/{eventId}` | EventsController | UpdateEvent |
| DELETE `/api/events/{eventId}` | EventsController | DeleteEvent |
| GET `/api/events/{eventId}/categories` | CategoriesController | GetCategories |
| POST `/api/events/{eventId}/categories` | CategoriesController | CreateCategory |
| PUT `/api/categories/{categoryId}` | CategoriesController | UpdateCategory |
| DELETE `/api/categories/{categoryId}` | CategoriesController | DeleteCategory |
| POST `/api/enrolments` | EnrolmentsController | CreateEnrolment |
| GET `/api/enrolments/mine` | EnrolmentsController | GetMyEnrolments |
| GET `/api/events/{eventId}/enrolments` | EnrolmentsController | GetEventEnrolments |
| GET `/api/enrolments/{enrolmentId}` | EnrolmentsController | GetEnrolment |
| DELETE `/api/enrolments/{enrolmentId}` | EnrolmentsController | CancelEnrolment |
| POST `/api/enrolments/{enrolmentId}/result` | ResultsController | CreateResult |
| GET `/api/results/mine` | ResultsController | GetMyResults |
| GET `/api/events/{eventId}/results` | ResultsController | GetEventResults |
| GET `/api/results/{resultId}` | ResultsController | GetResult |
| PUT `/api/results/{resultId}` | ResultsController | UpdateResult |

## Security and validation summary

- Authentication uses an HTTP-only session cookie and server-side session state.
- Passwords are hashed using ASP.NET Core's `PasswordHasher<AppUser>`.
- `[Authorize]` protects private endpoints; role attributes enforce Organiser/Participant separation.
- Organiser event ownership is checked for event, category, enrolment and result operations.
- Participants can only access their own profile, enrolments and results.
- Database uniqueness constraints prevent duplicate email addresses, duplicate enrolments in one event, and multiple results for one enrolment.
- A composite foreign key ensures that an enrolment's category belongs to the same event.
