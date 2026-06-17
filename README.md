What to watch tonight
A console application in C# that recommends movies based on the user's
preferences, or lets them query a movie database with custom filters.
What it does?
On start, the program loads a movie database from a CSV file and offers
two modes:
1. Recommendations: asks a few optional questions (genre, minimum
   year, minimum rating, short vs. long, Czech dub only) and returns
   top matches. Small amount of randomness is added so the same input
   doesn't always produce the exact same answers.
2. Query: filter the database manually by genre, year range, rating
   range and runtime range, with possible sorting by any field.
All preference questions are optional, leaving an answer blank (or
entering `0` where prompted) skips that given filter.
How to build and run?
Run it from the project directory. 'data.csv' must be present in the
working dir.
Project structure:
- 'Movie': immutable data model for one film
- 'CsvLoader': reads and parses the CSV into 'Movie' objects
- 'PreferencesCollector': gathers user preferences via console prompts
- 'Scorer': scores movies against preferences, returns the top choices
- 'QueryEngine': manual filtering / sorting mode
- 'Program': entry point / menu
