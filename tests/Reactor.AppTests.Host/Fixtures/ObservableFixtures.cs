using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Component = Microsoft.UI.Reactor.Core.Component;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

internal static class ObservableFixtures
{
    // ── Test models ────────────────────────────────────────────────

    private class PersonModel : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private int _age;
        public int Age
        {
            get => _age;
            set
            {
                if (_age != value)
                {
                    _age = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Age)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // ── UseObservable: INPC object updates reflected in UI ────────

    internal class UseObservableRerenderComponent : Component
    {
        private readonly PersonModel _person = new() { Name = "Alice", Age = 30 };

        public override Element Render()
        {
            UseObservable(_person);
            return VStack(
                TextBlock($"Name: {_person.Name}").AutomationId("NameDisplay"),
                TextBlock($"Age: {_person.Age}").AutomationId("AgeDisplay"),
                Button("ChangeName", () => _person.Name = "Bob").AutomationId("ChangeNameBtn"),
                Button("ChangeAge", () => _person.Age = 42).AutomationId("ChangeAgeBtn")
            );
        }
    }

    internal static Element UseObservable_Rerender(RenderContext ctx) =>
        Component<UseObservableRerenderComponent>();

    // ── UseObservable: external mutation ──────────────────────────
    // Note: External mutation testing (mutating without button) is not feasible
    // in Appium. We expose a button to simulate the mutation instead.

    internal class UseObservableExternalComponent : Component
    {
        private readonly PersonModel _person = new() { Name = "Alice", Age = 30 };

        public override Element Render()
        {
            UseObservable(_person);
            return VStack(
                TextBlock($"Name: {_person.Name}").AutomationId("ExtNameDisplay"),
                Button("MutateName", () => _person.Name = "Charlie").AutomationId("MutateNameBtn")
            );
        }
    }

    internal static Element UseObservable_ExternalMutation(RenderContext ctx) =>
        Component<UseObservableExternalComponent>();

    // ── UseObservableProperty: fine-grained rerender ──────────────

    internal class UseObservablePropertyComponent : Component
    {
        private readonly PersonModel _person = new() { Name = "Alice", Age = 30 };

        public override Element Render()
        {
            var name = UseObservableProperty(_person, p => p.Name, nameof(PersonModel.Name));
            return VStack(
                TextBlock($"Name: {name}").AutomationId("PropNameDisplay"),
                Button("ChangeName", () => _person.Name = "Bob").AutomationId("PropChangeNameBtn"),
                Button("ChangeAge", () => _person.Age = 99).AutomationId("PropChangeAgeBtn")
            );
        }
    }

    internal static Element UseObservableProperty_FineGrained(RenderContext ctx) =>
        Component<UseObservablePropertyComponent>();

    // ── UseCollection: list add/remove reflected in UI ────────────

    internal class UseCollectionComponent : Component
    {
        private readonly ObservableCollection<string> _items = new() { "Apple", "Banana" };

        public override Element Render()
        {
            var list = UseCollection(_items);
            return VStack(
                TextBlock($"Count: {list.Count}").AutomationId("CollectionCount"),
                VStack(list.Select((item, idx) =>
                    TextBlock(item).AutomationId($"CollItem{idx}")
                ).ToArray()),
                Button("AddCherry", () => _items.Add("Cherry")).AutomationId("AddCherryBtn"),
                Button("RemoveFirst", () => { if (_items.Count > 0) _items.RemoveAt(0); }).AutomationId("RemoveFirstBtn"),
                Button("Clear", () => _items.Clear()).AutomationId("ClearBtn")
            );
        }
    }

    internal static Element UseCollection_ListUpdates(RenderContext ctx) =>
        Component<UseCollectionComponent>();

    // ── UseObservable: source swap between renders ────────────────

    internal class UseObservableSourceSwapComponent : Component
    {
        private readonly PersonModel _person1 = new() { Name = "Alice" };
        private readonly PersonModel _person2 = new() { Name = "Bob" };

        public override Element Render()
        {
            var (useSecond, setUseSecond) = UseState(false);
            var current = useSecond ? _person2 : _person1;
            UseObservable(current);

            return VStack(
                TextBlock($"Name: {current.Name}").AutomationId("SwapNameDisplay"),
                Button("Swap", () => setUseSecond(!useSecond)).AutomationId("SwapBtn"),
                Button("MutateCurrent", () => current.Name = current.Name + " Updated")
                    .AutomationId("MutateCurrentBtn")
            );
        }
    }

    internal static Element UseObservable_SourceSwap(RenderContext ctx) =>
        Component<UseObservableSourceSwapComponent>();
}
