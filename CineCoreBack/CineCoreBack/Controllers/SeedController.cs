using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CineCoreBack.Models;
using System.Text;

namespace CineCoreBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly DbConfig _context;

        public SeedController(DbConfig context)
        {
            _context = context;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunSeed()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "imdb_top_1000.csv");
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Файл imdb_top_1000.csv не знайдено." });

            var lines = await System.IO.File.ReadAllLinesAsync(filePath);

            // Витягуємо всі існуючі імейли, щоб не створити дублікати
            var existingEmails = new HashSet<string>(await _context.Users.Select(u => u.Email).ToListAsync());

            var usersDict = new Dictionary<string, User>();
            var genresDict = new Dictionary<string, Genre>();

            // ==========================================
            // ВИПРАВЛЕННЯ ДЛЯ ЖАНРІВ (БЕЗПЕЧНИЙ СЛОВНИК)
            // ==========================================
            var genresFromDb = await _context.Genres.ToListAsync();
            var existingGenres = new Dictionary<string, Genre>(StringComparer.OrdinalIgnoreCase);

            foreach (var g in genresFromDb)
            {
                // Додаємо жанр у словник тільки якщо такого там ще немає
                if (!existingGenres.ContainsKey(g.Name))
                {
                    existingGenres[g.Name] = g;
                }
            }
            // ==========================================

            var projects = new List<Project>();
            var projectMembers = new List<ProjectMember>();
            var projectGenres = new List<ProjectGenre>();
            var actorsList = new List<Actor>();

            var random = new Random();
            var themes = new[] { "theme-teal", "theme-cyber", "theme-sunset", "theme-mono", "theme-ocean", "theme-lavender", "theme-ruby" };

            // Починаємо з 1, щоб пропустити заголовок CSV
            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i];
                if (string.IsNullOrWhiteSpace(row)) continue;

                var fields = ParseCsvRow(row);
                if (fields.Length < 14) continue;

                string title = fields[1].Trim();
                string genreStr = fields[5].Trim();
                string overview = fields[7].Trim();
                string directorName = string.IsNullOrWhiteSpace(fields[9]) ? "Unknown Director" : fields[9].Trim();
                string[] starNames = { fields[10].Trim(), fields[11].Trim(), fields[12].Trim(), fields[13].Trim() };

                // 1. РЕЖИСЕР
                if (!usersDict.TryGetValue(directorName, out var directorUser))
                {
                    directorUser = CreateUserSafe(directorName, random, themes, existingEmails);
                    usersDict[directorName] = directorUser;
                }

                // 2. ПРОЄКТ
                var project = new Project
                {
                    Title = title.Length > 200 ? title.Substring(0, 200) : title,
                    Synopsis = overview,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(random.Next(30, 365))),
                    Owner = directorUser,
                    CreatedAt = DateTime.UtcNow
                };
                projects.Add(project);

                // 3. ЖАНРИ
                var splitGenres = genreStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct();
                foreach (var gName in splitGenres)
                {
                    if (!existingGenres.TryGetValue(gName, out var genreObj) && !genresDict.TryGetValue(gName, out genreObj))
                    {
                        genreObj = new Genre { Name = gName };
                        genresDict[gName] = genreObj;
                    }
                    projectGenres.Add(new ProjectGenre { Project = project, Genre = genreObj });
                }

                // 4. АКТОРИ
                var starsInMovie = new HashSet<string>();
                foreach (var starName in starNames)
                {
                    if (string.IsNullOrWhiteSpace(starName) || !starsInMovie.Add(starName)) continue;

                    if (!usersDict.TryGetValue(starName, out var starUser))
                    {
                        starUser = CreateUserSafe(starName, random, themes, existingEmails);
                        usersDict[starName] = starUser;
                        actorsList.Add(new Actor { User = starUser, Characteristics = "{}" });
                    }
                    else if (!actorsList.Any(a => a.User == starUser))
                    {
                        actorsList.Add(new Actor { User = starUser, Characteristics = "{}" });
                    }

                    projectMembers.Add(new ProjectMember
                    {
                        Project = project,
                        User = starUser,
                        InvitedEmail = starUser.Email,
                        SysRole = "actor",
                        JobTitle = "Cast",
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }

            // ЗБЕРЕЖЕННЯ В БД
            _context.Users.AddRange(usersDict.Values);
            _context.Actors.AddRange(actorsList);
            _context.Genres.AddRange(genresDict.Values);
            _context.Projects.AddRange(projects);
            _context.ProjectGenres.AddRange(projectGenres);
            _context.ProjectMembers.AddRange(projectMembers);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Success! Demo data has been imported into your database.",
                projectsCount = projects.Count,
                usersCount = usersDict.Count,
                actorsCount = actorsList.Count,
                crewCount = projectMembers.Count,
                genresCount = genresDict.Count
            });
        }

        // ==========================================
        // ВИПРАВЛЕННЯ ДЛЯ ДАТИ (UTC) ТА ІМЕЙЛІВ
        // ==========================================
        private User CreateUserSafe(string fullName, Random random, string[] themes, HashSet<string> existingEmails)
        {
            var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "Actor";

            string email;
            do
            {
                email = $"{firstName.ToLower()}.{lastName.ToLower().Replace(" ", "")}{random.Next(1, 99999)}@cinecore.com";
            } while (existingEmails.Contains(email));

            existingEmails.Add(email);

            return new User
            {
                FirstName = firstName.Length > 50 ? firstName.Substring(0, 50) : firstName,
                LastName = lastName.Length > 50 ? lastName.Substring(0, 50) : lastName,
                Email = email.Length > 255 ? email.Substring(0, 255) : email,
                PasswordHash = "password123",
                // ВАЖЛИВО: SpecifyKind Utc, щоб PostgreSQL не видавав помилку
                Birthday = DateTime.SpecifyKind(new DateTime(1950, 1, 1).AddDays(random.Next(0, 25000)), DateTimeKind.Utc),
                PhoneNum = $"+380{random.Next(100000000, 999999999)}",
                AvatarTheme = themes[random.Next(themes.Length)],
                RegisteredAt = DateTime.UtcNow
            };
        }

        // ==========================================
        // ВИПРАВЛЕННЯ ПАРСИНГУ CSV (З ЛАПКАМИ)
        // ==========================================
        private string[] ParseCsvRow(string row)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '\"')
                    {
                        currentField.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString());
            return result.ToArray();
        }
    }
}