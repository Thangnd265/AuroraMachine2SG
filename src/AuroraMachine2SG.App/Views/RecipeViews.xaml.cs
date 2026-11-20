using System.Windows;
using System.Windows.Controls;
using AuroraMachine2SG.App.Dialogs;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class RecipeViews : UserControl
{
    public RecipeViews() => InitializeComponent();

    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void SaveRecipe_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.RecipeVM.SaveAll();
        Vm.StatusBanner = "Saved MainRecipe & WMX3PositionRecipe to _data/_config_recipe/";
    }

    private void AddTab_Click(object s, RoutedEventArgs e)
    {
        if (Vm is not null && Window.GetWindow(this) is Window w)
        {
            var name = OperationalDialogs.PromptText(w, "Add Teaching Tab", "Enter new Teaching Tab name:", "New_Tab");
            if (!string.IsNullOrWhiteSpace(name)) Vm.RecipeVM.AddTab(name);
        }
    }

    private void DelTab_Click(object s, RoutedEventArgs e) => Vm?.RecipeVM.DeleteSelectedTab();
    private void AddMotion_Click(object s, RoutedEventArgs e) => Vm?.RecipeVM.AddMotionCardToCurrentTab();
    private void Reload_Click(object s, RoutedEventArgs e) => Vm?.RecipeVM.Reload();

    private void CloneTo_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.RecipeVM.ActiveTabCards.FirstOrDefault() is { } card && Window.GetWindow(this) is Window w)
        {
            var target = OperationalDialogs.PromptText(w, "Clone Motion Card To Tab", $"Clone '{card.MotionId}' into target Tab:", "Cloned_Stage");
            if (!string.IsNullOrWhiteSpace(target))
            {
                Vm.RecipeVM.CloneCardToTab(card, target);
                Vm.StatusBanner = $"Cloned {card.MotionId} to tab '{target}'.";
            }
        }
    }
}
