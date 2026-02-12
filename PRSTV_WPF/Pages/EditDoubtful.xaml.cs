using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Linq;
using PRSTV_WPF.Models;

namespace PRSTV_WPF.Pages
{
    /// <summary>
    /// Interaction logic for EditDoubtful.xaml
    /// </summary>
    public partial class EditDoubtful : Page
    {
        private readonly BallotPaper _ballot;
        private const string ImageBaseUrl = "https://ballot-imgs.s3.us-east-1.amazonaws.com/raw-images/";

        private readonly BallotPreferenceService _prefService;
        private readonly CandidateService _candidateService;

        private ObservableCollection<BallotPreferenceRow> _rows = new();

        public EditDoubtful(BallotPaper ballot)
        {
            InitializeComponent();

            _ballot = ballot;

            _prefService = new BallotPreferenceService(SupabaseClientFactory.Client);
            _candidateService = new CandidateService(SupabaseClientFactory.Client);

            DataContext = _ballot;
            Loaded += BallotDetailPage_Loaded;
        }

        private async void BallotDetailPage_Loaded(object sender, RoutedEventArgs e)
        {

            StatusText.Text = "Loading...";

            LoadBallotImage();

            // load existing preferences for this ballot
            var prefs = await _prefService.GetByRandomBallotIdAsync(_ballot.RandomBallotId);

            // IMPORTANT: load ALL candidates (you need this method in CandidateService)
            var candidates = await _candidateService.GetAllAsync();

            // quick lookup preference by CandidateId
            var prefByCandidateId = prefs.ToDictionary(p => p.CandidateId, p => p);

            var vmRows = candidates
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .Select(c =>
                {
                    if (prefByCandidateId.TryGetValue(c.Id, out var p))
                    {
                        return new BallotPreferenceRow
                        {
                            Id = p.Id,
                            CandidateId = p.CandidateId,
                            RandomBallotId = p.RandomBallotId,
                            CandidateName = $"{c.FirstName} {c.LastName}".Trim(),
                            Preference = p.Preference
                        };
                    }

                    // Candidate has no preference yet
                    return new BallotPreferenceRow
                    {
                        Id = 0,
                        CandidateId = c.Id,
                        RandomBallotId = _ballot.RandomBallotId,
                        CandidateName = $"{c.FirstName} {c.LastName}".Trim(),
                        Preference = null
                    };
                })
                .ToList();

            _rows = new ObservableCollection<BallotPreferenceRow>(vmRows);
            PreferencesGrid.ItemsSource = _rows;

            // setup ballot state dropdown
            BallotStateCombo.ItemsSource = new[] { "Valid", "Doubtful", "Spoiled" };
            BallotStateCombo.SelectedItem = _ballot.BallotState; // assuming BallotState is a string in model

            StatusText.Text = $"{_rows.Count} candidates loaded.";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            PreferencesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            PreferencesGrid.CommitEdit(DataGridEditingUnit.Row, true);

            // Validate filled-in preferences
            var filled = _rows.Where(r => r.Preference.HasValue).ToList();

            if (filled.Any(r => r.Preference!.Value <= 0))
            {
                MessageBox.Show("Preferences must be a positive number (1, 2, 3, ...), or left blank.");
                return;
            }

            var dupes = filled
                .GroupBy(r => r.Preference!.Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dupes.Any())
            {
                MessageBox.Show("Duplicate preference numbers found: " + string.Join(", ", dupes));
                return;
            }

            try
            {
                StatusText.Text = "Updating...";

                // 1) Update ballot state
                var selectedState = BallotStateCombo.SelectedItem?.ToString() ?? "Doubtful";
                var ballotService = new BallotPaperService(SupabaseClientFactory.Client);
                await ballotService.UpdateBallotStateAsync(_ballot.Id, Enum.Parse<BALLOT_SATE>(selectedState));

                // keep UI model in sync (optional)
                _ballot.BallotState = selectedState;

                // 2) Preferences: split into inserts/updates/deletes
                var toInsert = _rows
                    .Where(r => r.Id == 0 && r.Preference.HasValue)
                    .Select(r => new BallotPreference
                    {
                        CandidateId = r.CandidateId,
                        RandomBallotId = r.RandomBallotId,
                        Preference = r.Preference!.Value
                    })
                    .ToList();

                var toUpdate = _rows
                    .Where(r => r.Id > 0 && r.Preference.HasValue)
                    .Select(r => new BallotPreference
                    {
                        Id = r.Id,
                        CandidateId = r.CandidateId,
                        RandomBallotId = r.RandomBallotId,
                        Preference = r.Preference!.Value
                    })
                    .ToList();

                var toDeleteIds = _rows
                    .Where(r => r.Id > 0 && !r.Preference.HasValue)
                    .Select(r => r.Id)
                    .ToList();

                await _prefService.ApplyPreferenceChangesAsync(toInsert, toUpdate, toDeleteIds);

                StatusText.Text = "Updated successfully.";
                MessageBox.Show("Ballot updated successfully.");
            }
            catch (Exception ex)
            {
                StatusText.Text = "Update failed.";
                MessageBox.Show($"Update failed: {ex.Message}");
            }
        }
        private void LoadBallotImage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_ballot.ImageUrl))
                {
                    BallotImage.Source = null;
                    return;
                }

                var fullUrl = ImageBaseUrl + _ballot.ImageUrl.Trim();

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullUrl, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad; // avoid file locking / keep it simple
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // optional
                bmp.EndInit();

                BallotImage.Source = bmp;
            }
            catch (Exception ex)
            {
                BallotImage.Source = null;
                StatusText.Text = "Image failed to load.";
                // Optional: MessageBox.Show($"Image failed: {ex.Message}");
            }
        }

    }
}
