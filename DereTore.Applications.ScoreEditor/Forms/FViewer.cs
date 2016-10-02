﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DereTore.ACB;
using DereTore.Applications.ScoreEditor.Model;
using DereTore.HCA;
using DereTore.StarlightStage;
//using Timer = System.Timers.Timer;
using Timer = DereTore.Applications.ScoreEditor.ThreadingTimer;

namespace DereTore.Applications.ScoreEditor.Forms {
    public partial class FViewer : Form {

        public FViewer() {
            InitializeComponent();
            RegisterEventHandlers();
            CheckForIllegalCrossThreadCalls = false;
        }

        private bool CheckPlayEnvironment() {
            if (txtAudioFileName.TextLength == 0) {
                this.ShowMessageBox("Please select the ACB file.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (txtScoreFileName.TextLength == 0) {
                this.ShowMessageBox("Please select the score file.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            var scoreFileName = txtScoreFileName.Text;
            var scoreFileExtension = new FileInfo(scoreFileName).Extension.ToLowerInvariant();
            if (scoreFileExtension == ExtensionBdb) {
                string[] scoreNames;
                var isScoreFile = Score.IsScoreFile(txtScoreFileName.Text, out scoreNames);
                if (!isScoreFile) {
                    this.ShowMessageBox($"The file '{scoreFileName}' is not a score database file.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
                if (!Score.ContainsDifficulty(scoreNames, (Difficulty)(cboDifficulty.SelectedIndex + 1))) {
                    this.ShowMessageBox($"The file '{scoreFileName}' does not contain required difficulty '{cboDifficulty.SelectedItem}'.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            } else if (scoreFileExtension == ExtensionCsv) {
                // Don't have an idea how to fully check the file.
            } else {
                this.ShowMessageBox($"The file {scoreFileName} is neither a score database or a single score file.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }

        private void ClosePlayers() {
            SfxManager.Instance.Dispose();
        }

        private bool ConfirmNoteEdition(NoteEdition edition, Note note, Note newValue) {
            var result = DialogResult.Yes;
            var prompts = new List<string>();
            switch (edition) {
                case NoteEdition.Remove:
                    if (note.IsSync) {
                        prompts.Add("* The note is a sync note. Removing it will break the sync property of its paired sync note.");
                    }
                    break;
                case NoteEdition.Edit:
                    if (note.IsSync && !note.HitTiming.Equals(newValue.HitTiming)) {
                        prompts.Add("* The note is a sync note. Editing it will break the sync property of its paired sync note.");
                    }
                    break;
                case NoteEdition.ResetType:
                    if (note.IsSync) {
                        prompts.Add("* The note is a sync note. Resetting its type will break the sync property of its paired sync note.");
                    }
                    break;
                case NoteEdition.ResetTiming:
                    if (note.IsSync && !note.HitTiming.Equals(newValue.HitTiming)) {
                        prompts.Add("* The note is a sync note. Resetting its timing will break the sync property of its paired sync note.");
                    }
                    break;
                default:
                    break;
            }
            if (prompts.Count > 0) {
                var prompt = $"Please confirm your action with note ID #{note.Id}." + Environment.NewLine + prompts.BuildString(Environment.NewLine);
                result = this.ShowMessageBox(prompt, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            }
            return result == DialogResult.Yes;
        }

        private void InitializeControls() {
            openFileDialog.CheckFileExists = true;
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Multiselect = false;
            openFileDialog.ReadOnlyChecked = false;
            openFileDialog.ShowReadOnly = false;
            openFileDialog.ValidateNames = true;
            cboDifficulty.SelectedIndex = 0;
            cboSoundEffect.SelectedIndex = 0;
            SetControlsEnabled(ViewerState.Initialized);
        }

        private void PreloadNoteSounds() {
            const int sfxTypeCount = 4;
            for (var i = 0; i < sfxTypeCount; ++i) {
                var acbFileName = string.Format(SoundEffectAcbFileNameFormat, i.ToString("00"));
                using (var fileStream = File.Open(acbFileName, FileMode.Open, FileAccess.Read)) {
                    using (var acbFile = AcbFile.FromStream(fileStream, false)) {
                        foreach (var hcaName in new[] { TapHcaName, FlickHcaName }) {
                            using (var dataStream = acbFile.OpenDataStream(hcaName)) {
                                SfxManager.Instance.PreloadHca(dataStream, $"{acbFileName}/{hcaName}");
                            }
                        }
                    }
                }
            }
        }

        private void SetControlsEnabled(ViewerState state) {
            switch (state) {
                case ViewerState.Initialized:
                    btnSelectAudio.Enabled = true;
                    btnSelectScore.Enabled = true;
                    cboDifficulty.Enabled = true;
                    cboSoundEffect.Enabled = true;
                    progress.Enabled = false;
                    btnScoreLoad.Enabled = true;
                    btnScoreUnload.Enabled = false;
                    btnPlay.Enabled = false;
                    btnPause.Enabled = false;
                    btnStop.Enabled = false;
                    propertyGrid.Enabled = false;
                    tsbNoteCreate.Enabled = false;
                    tsbNoteEdit.Enabled = false;
                    tsbNoteRemove.Enabled = false;
                    tsbMakeSync.Enabled = false;
                    tsbMakeFlick.Enabled = false;
                    tsbMakeHold.Enabled = false;
                    tsbResetToTap.Enabled = false;
                    tsbRetimingToNow.Enabled = false;
                    tsbScoreCreate.Enabled = false;
                    tsbScoreSave.Enabled = false;
                    tsbScoreSaveAs.Enabled = false;
                    break;
                case ViewerState.Loaded:
                    btnSelectAudio.Enabled = false;
                    btnSelectScore.Enabled = false;
                    cboDifficulty.Enabled = false;
                    cboSoundEffect.Enabled = false;
                    progress.Enabled = true;
                    btnScoreLoad.Enabled = false;
                    btnScoreUnload.Enabled = true;
                    btnPlay.Enabled = true;
                    btnPause.Enabled = false;
                    btnStop.Enabled = false;
                    propertyGrid.Enabled = true;
                    tsbNoteCreate.Enabled = true;
                    tsbNoteEdit.Enabled = false;
                    tsbNoteRemove.Enabled = false;
                    tsbMakeSync.Enabled = false;
                    tsbMakeFlick.Enabled = false;
                    tsbMakeHold.Enabled = false;
                    tsbResetToTap.Enabled = false;
                    tsbRetimingToNow.Enabled = false;
                    tsbScoreCreate.Enabled = false;
                    tsbScoreSave.Enabled = true;
                    tsbScoreSaveAs.Enabled = false;
                    break;
                case ViewerState.LoadedAndPlaying:
                    btnSelectAudio.Enabled = false;
                    btnSelectScore.Enabled = false;
                    cboDifficulty.Enabled = false;
                    cboSoundEffect.Enabled = false;
                    progress.Enabled = true;
                    btnScoreLoad.Enabled = false;
                    btnScoreUnload.Enabled = true;
                    btnPlay.Enabled = false;
                    btnPause.Enabled = true;
                    btnStop.Enabled = true;
                    propertyGrid.Enabled = true;
                    break;
                case ViewerState.LoadedAndPaused:
                    btnSelectAudio.Enabled = false;
                    btnSelectScore.Enabled = false;
                    cboDifficulty.Enabled = false;
                    cboSoundEffect.Enabled = false;
                    progress.Enabled = true;
                    btnScoreLoad.Enabled = false;
                    btnScoreUnload.Enabled = true;
                    btnPlay.Enabled = true;
                    btnPause.Enabled = false;
                    btnStop.Enabled = true;
                    propertyGrid.Enabled = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private static readonly string AudioFilter = "All Supported Audio Formats (*.wav;*.acb;*.hca)|*.wav;*.acb;*.hca|ACB Archive (*.acb)|*.acb|Wave Audio (*.wav)|*.wav|HCA Audio (*.hca)|*.hca";
        private static readonly string ScoreFilter = "All Supported Score Formats (*.bdb;*.csv)|*.bdb;*.csv|Score Database (*.bdb)|*.bdb|Single Score (*.csv)|*.csv";

        private static readonly string ExtensionAcb = ".acb";
        private static readonly string ExtensionWav = ".wav";
        private static readonly string ExtensionHca = ".hca";
        private static readonly string ExtensionBdb = ".bdb";
        private static readonly string ExtensionCsv = ".csv";

        private static readonly string SongTipFormat = "Song: {0}";

        private static readonly string SoundEffectAcbFileNameFormat = "Resources/SFX/se_live_{0}.acb";
        private static readonly string FlickHcaName = "se_live_flic_perfect.hca";
        private static readonly string TapHcaName = "se_live_tap_perfect.hca";
        private string _currentFlickHcaFileName;
        private string _currentTapHcaFileName;

        private readonly Timer timer = new Timer(5);

        private LiveMusicPlayer _player;
        private Score _score;
        private Stream _audioFileStream;

        private int _userSeekingStack = 0;

        private static readonly DecodeParams DefaultCgssDecodeParams = new DecodeParams {
            Key1 = CgssCipher.Key1,
            Key2 = CgssCipher.Key2
        };

        private bool _codeValueChange;
        private readonly object _liveMusicSyncObject = new object();
    }
}
