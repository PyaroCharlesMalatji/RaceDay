# RaceDay API (Part 2)

A RESTful ASP.NET Core Web API for managing running, walking and cycling events.

## Setup and run
1. Install Visual Studio 2022 with the ASP.NET workload, and .NET 8 SDK.
2. GitHub : " "
3. Open "RaceDay.sln".
4. Check the connection string in "RaceDayApi/appsettings.json" (uses LocalDB).
5. In Package Manager Console run "Update-Database".
6. Press F5, then use Swagger at "/swagger".
7. Run tests: Test > Run All Tests. 

## Roles
- **Organiser**: creates, updates and deletes their own events, defines categories, views event enrolments, records results.
- **Participant**: views events and categories, enrols in an event with a category, views their own results and enrolments.
Both roles can register, log in and manage their own profile.


## YouTube video
