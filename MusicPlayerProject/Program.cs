using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave; // For audio playback

class Program
{
    static bool isPaused = false;
    static IWavePlayer waveOutDevice;
    static MediaFoundationReader audioFile;

    static async Task Main(string[] args)
    {
        const string clientId = "134b5460"; 
        HttpClient httpClient = new HttpClient();
        List<Song> searchResults = new();



        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Console Music Player ===");
            Console.WriteLine("1. Search for Songs");
            Console.WriteLine("2. Exit");
            Console.WriteLine("Use Arrow Keys to Navigate. Press Enter to Select.");

            int menuChoice = NavigateMenu(2);
            if (menuChoice == 2) break;

            if (menuChoice == 1)
            {
                Console.WriteLine("Enter 'A' to search by Artist, 'B' to search by Album, or 'C' to search by Song Name:");
                string option = Console.ReadLine().Trim().ToUpper();

                string query = string.Empty;
                string parameter = string.Empty;

                switch (option)
                {
                    case "A":
                        Console.WriteLine("Enter the Artist name:");
                        query = Console.ReadLine().Trim();
                        parameter = "artist_name";  // Use artist_name for searching by artist
                        break;

                    case "B":
                        Console.WriteLine("Enter the Album name:");
                        query = Console.ReadLine().Trim();
                        parameter = "album_name";  // Use album_name for searching by album
                        break;

                    case "C":
                        Console.WriteLine("Enter the Song name:");
                        query = Console.ReadLine().Trim();
                        parameter = "namesearch";  // Use track_name for searching by song title
                        break;

                    default:
                        Console.WriteLine("Invalid option. Try again.");
                        continue;
                }

                // Perform the search
                string url = $"https://api.jamendo.com/v3.0/tracks/?client_id={clientId}&{parameter}={Uri.EscapeDataString(query)}&format=jsonpretty&limit=10";
                try
                {
                    string response = await httpClient.GetStringAsync(url);
                    JsonDocument jsonDocument = JsonDocument.Parse(response);
                    var tracks = jsonDocument.RootElement.GetProperty("results");

                    if (tracks.GetArrayLength() == 0)
                    {
                        Console.WriteLine("No results found. Press any key to return to the menu.");
                        Console.ReadKey();
                        continue;
                    }

                    searchResults.Clear();
                    Console.Clear();
                    Console.WriteLine("Search Results:\n");

                    int id = 1;
                    foreach (var track in tracks.EnumerateArray())
                    {
                        string trackName = track.GetProperty("name").GetString();
                        string artistName = track.GetProperty("artist_name").GetString();
                        string albumName = track.GetProperty("album_name").GetString();  
                        int duration = track.GetProperty("duration").GetInt32();
                        string audioURL = track.GetProperty("audio").GetString();

                        searchResults.Add(new Song
                        {
                            Id = id,
                            Title = trackName,
                            Artist = artistName,
                            Duration = TimeSpan.FromSeconds(duration),
                            AudioURL = audioURL,
                            Album = albumName  // Store album info for display
                        });

                        Console.WriteLine($"ID: {id} | Track: {trackName}\nArtist: {artistName} | Album: {albumName} | Duration: {duration / 60:D2}:{duration % 60:D2} minutes\n");
                        id++;
                    }

                    Console.WriteLine("Enter a Song ID to Play or Press 0 to Return:");
                    int songId = int.Parse(Console.ReadLine() ?? "0");
                    if (songId > 0 && songId <= searchResults.Count)
                    {
                        PlaySongVisualization(searchResults[songId - 1]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }

    public static void PlaySongVisualization(Song song)
    {
        Console.Clear();
        Console.WriteLine($"Now Playing: {song.Title} by {song.Artist}");

        int duration = (int)song.Duration.TotalSeconds;
        int currentTime = 0;

        // Start audio playback
        PlayAudio(song.AudioURL);

        while (currentTime <= duration)
        {
            if (isPaused)
            {
                // If paused, display the paused message and don't update the visualization or progress bar
                Console.Clear();
                Console.WriteLine($"Now Playing: {song.Title} by {song.Artist} - PAUSED");
                DrawPausedVisualizer();
                DrawProgressBar(currentTime, duration);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Spacebar)
                    {
                        // Toggle the pause/resume
                        isPaused = !isPaused;

                        if (!isPaused)
                        {
                            // Resume audio when paused
                            waveOutDevice.Play();  // Assuming waveOutDevice is used for audio playback
                        }
                    }
                    else if (key == ConsoleKey.Escape)
                    {
                        // Stop the audio when escape key is pressed
                        StopAudio();
                        return; // Exit the playback
                    }
                }

                Thread.Sleep(500); // Sleep while paused
                continue;
            }

            // When not paused, update the visualization and progress bar
            Console.Clear();
            Console.WriteLine($"Now Playing: {song.Title} by {song.Artist}");
            DrawVisualizer();
            DrawProgressBar(currentTime, duration);

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Spacebar)
                {
                    // Toggle the pause/resume
                    isPaused = !isPaused;

                    if (isPaused)
                    {
                        // Pause the audio
                        waveOutDevice.Pause();  // Assuming waveOutDevice is used for audio playback
                    }
                }
                else if (key == ConsoleKey.Escape)
                {
                    // Stop the audio when escape key is pressed
                    StopAudio();
                    return; // Exit the playback
                }
            }

            currentTime++;  // Increment current time by one second (this represents the progress)
            Thread.Sleep(1000);  // Wait 1 second before the next iteration
        }

        // Ensure audio stops after the song ends
        StopAudio();
    }

    private static void DrawPausedVisualizer()
    {
        Console.WriteLine("Visualization Paused...");
    }




    public static void PlayAudio(string url)
    {
        try
        {
            StopAudio(); // Stop any currently playing audio
            waveOutDevice = new WaveOutEvent();
            audioFile = new MediaFoundationReader(url);
            waveOutDevice.Init(audioFile);
            waveOutDevice.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing audio: {ex.Message}");
        }
    }

    public static void StopAudio()
    {
        waveOutDevice?.Stop();
        waveOutDevice?.Dispose();
        audioFile?.Dispose();
        waveOutDevice = null;
        audioFile = null;
    }

    private static void DrawVisualizer()
    {
        Random rnd = new Random();
        for (int i = 0; i < 10; i++)
        {
            int height = rnd.Next(1, 15);
            DrawBar(height);
        }
    }

    private static void DrawBar(int height)
    {
        for (int i = 0; i < height; i++)
        {
            Console.Write("||");
        }
        Console.WriteLine();
    }

    private static void DrawProgressBar(int current, int total)
    {
        int barLength = 50;
        int progress = (int)((float)current / total * barLength);

        Console.Write("[");
        for (int i = 0; i < barLength; i++)
        {
            if (i < progress)
                Console.Write("=");
            else
                Console.Write(" ");
        }
        Console.Write($"] {current}/{total}s");
    }

    private static int NavigateMenu(int optionsCount)
    {
        int currentIndex = 0;
        ConsoleKey key;

        do
        {
            for (int i = 0; i < optionsCount; i++)
            {
                if (i == currentIndex)
                    Console.WriteLine($"> Search {i + 1}");
                else
                    Console.WriteLine($"  Exit {i + 1}");
            }

            key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.UpArrow) currentIndex = (currentIndex == 0) ? optionsCount - 1 : currentIndex - 1;
            if (key == ConsoleKey.DownArrow) currentIndex = (currentIndex == optionsCount - 1) ? 0 : currentIndex + 1;

            Console.Clear();
        } while (key != ConsoleKey.Enter);

        return currentIndex + 1;
    }
}

class Song
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public TimeSpan Duration { get; set; }
    public string AudioURL { get; set; }
    public string Album { get; set; }  
}
