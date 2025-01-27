// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Transition screen for the editor.
    /// Used to avoid backing out to main menu/song select when switching difficulties from within the editor.
    /// </summary>
    public class EditorLoader : ScreenWithBeatmapBackground
    {
        public override float BackgroundParallaxAmount => 0.1f;

        public override bool AllowBackButton => false;

        public override bool HideOverlaysOnEnter => true;

        public override bool DisallowExternalBeatmapRulesetChanges => true;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        [CanBeNull]
        private ScheduledDelegate scheduledDifficultySwitch;

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            if (!resuming)
            {
                // the push cannot happen in OnEntering() or similar (even if scheduled), because the transition from main menu will look bad.
                // that is because this screen pushing the editor makes it no longer current, and OsuScreen checks if the screen is current
                // before enqueueing this screen's LogoArriving onto the logo animation sequence.
                pushEditor();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                new LoadingSpinner(true)
                {
                    State = { Value = Visibility.Visible },
                }
            });
        }

        public void ScheduleDifficultySwitch(BeatmapInfo beatmapInfo)
        {
            scheduledDifficultySwitch?.Cancel();
            ValidForResume = true;

            this.MakeCurrent();

            scheduledDifficultySwitch = Schedule(() =>
            {
                Beatmap.Value = beatmapManager.GetWorkingBeatmap(beatmapInfo);

                // This screen is a weird exception to the rule that nothing after song select changes the global beatmap.
                // Because of this, we need to update the background stack's beatmap to match.
                // If we don't do this, the editor will see a discrepancy and create a new background, along with an unnecessary transition.
                ApplyToBackground(b => b.Beatmap = Beatmap.Value);

                pushEditor();
            });
        }

        private void pushEditor()
        {
            this.Push(new Editor(this));
            ValidForResume = false;
        }

        public void CancelPendingDifficultySwitch()
        {
            scheduledDifficultySwitch?.Cancel();
            ValidForResume = false;
        }
    }
}
