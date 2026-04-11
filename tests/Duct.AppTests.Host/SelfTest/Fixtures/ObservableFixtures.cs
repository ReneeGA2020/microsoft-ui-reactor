using System.Collections.ObjectModel;
using System.ComponentModel;
using Duct;
using Duct.Core;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class ObservableFixtures
{
    // -- Test models --------------------------------------------------

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

    // -- UseObservable: INPC object updates reflected in UI -----------

    internal class UseObservable_Rerender(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person = new PersonModel { Name = "Alice", Age = 30 };

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                ctx.UseObservable(person);
                return VStack(
                    Text($"Name: {person.Name}"),
                    Text($"Age: {person.Age}"),
                    Button("ChangeName", () => person.Name = "Bob"),
                    Button("ChangeAge", () => person.Age = 42)
                );
            });

            await Harness.Render();

            H.Check("Observable_Rerender_InitialName",
                H.FindText("Name: Alice") is not null);
            H.Check("Observable_Rerender_InitialAge",
                H.FindText("Age: 30") is not null);

            H.ClickButton("ChangeName");
            await Harness.Render();

            H.Check("Observable_Rerender_NameUpdated",
                H.FindText("Name: Bob") is not null);
            H.Check("Observable_Rerender_AgeUnchanged",
                H.FindText("Age: 30") is not null);

            H.ClickButton("ChangeAge");
            await Harness.Render();

            H.Check("Observable_Rerender_AgeUpdated",
                H.FindText("Age: 42") is not null);
        }
    }

    // -- UseObservable: external mutation (not via button) ------------

    internal class UseObservable_ExternalMutation(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person = new PersonModel { Name = "Alice", Age = 30 };

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                ctx.UseObservable(person);
                return Text($"Name: {person.Name}");
            });

            await Harness.Render();

            H.Check("Observable_External_Initial",
                H.FindText("Name: Alice") is not null);

            person.Name = "Charlie";
            await Harness.Render();

            H.Check("Observable_External_AfterMutation",
                H.FindText("Name: Charlie") is not null);
        }
    }

    // -- UseObservableProperty: fine-grained rerender -----------------

    internal class UseObservableProperty_FineGrained(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person = new PersonModel { Name = "Alice", Age = 30 };
            int renderCount = 0;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var name = ctx.UseObservableProperty(person, p => p.Name, nameof(PersonModel.Name));
                renderCount++;
                return VStack(
                    Text($"Name: {name}"),
                    Text($"Renders: {renderCount}")
                );
            });

            await Harness.Render();

            H.Check("ObservableProp_FineGrained_Initial",
                H.FindText("Name: Alice") is not null);

            var rendersAfterMount = H.FindTextContaining("Renders:");

            person.Name = "Bob";
            await Harness.Render();

            H.Check("ObservableProp_FineGrained_NameUpdated",
                H.FindText("Name: Bob") is not null);

            int rendersAfterNameChange = renderCount;

            person.Age = 99;
            await Harness.Render();

            H.Check("ObservableProp_FineGrained_NoRerenderOnAge",
                renderCount == rendersAfterNameChange);
        }
    }

    // -- UseCollection: list add/remove reflected in UI ---------------

    internal class UseCollection_ListUpdates(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var items = new ObservableCollection<string> { "Apple", "Banana" };

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var list = ctx.UseCollection(items);
                return VStack(
                    Text($"Count: {list.Count}"),
                    VStack(list.Select(item => Text(item)).ToArray()),
                    Button("AddCherry", () => items.Add("Cherry")),
                    Button("RemoveFirst", () => { if (items.Count > 0) items.RemoveAt(0); }),
                    Button("Clear", () => items.Clear())
                );
            });

            await Harness.Render();

            H.Check("Collection_ListUpdates_InitialCount",
                H.FindText("Count: 2") is not null);
            H.Check("Collection_ListUpdates_InitialItems",
                H.FindText("Apple") is not null && H.FindText("Banana") is not null);

            H.ClickButton("AddCherry");
            await Harness.Render();

            H.Check("Collection_ListUpdates_AfterAdd",
                H.FindText("Count: 3") is not null);
            H.Check("Collection_ListUpdates_CherryAppears",
                H.FindText("Cherry") is not null);

            H.ClickButton("RemoveFirst");
            await Harness.Render();

            H.Check("Collection_ListUpdates_AfterRemove",
                H.FindText("Count: 2") is not null);
            H.Check("Collection_ListUpdates_AppleGone",
                H.FindText("Apple") is null);

            H.ClickButton("Clear");
            await Harness.Render();

            H.Check("Collection_ListUpdates_AfterClear",
                H.FindText("Count: 0") is not null);
        }
    }

    // -- UseObservable: source swap between renders -------------------

    internal class UseObservable_SourceSwap(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person1 = new PersonModel { Name = "Alice" };
            var person2 = new PersonModel { Name = "Bob" };

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (useSecond, setUseSecond) = ctx.UseState(false);
                var current = useSecond ? person2 : person1;
                ctx.UseObservable(current);

                return VStack(
                    Text($"Name: {current.Name}"),
                    Button("Swap", () => setUseSecond(!useSecond))
                );
            });

            await Harness.Render();

            H.Check("Observable_SourceSwap_Initial",
                H.FindText("Name: Alice") is not null);

            H.ClickButton("Swap");
            await Harness.Render();

            H.Check("Observable_SourceSwap_AfterSwap",
                H.FindText("Name: Bob") is not null);

            person2.Name = "Bob Updated";
            await Harness.Render();

            H.Check("Observable_SourceSwap_NewSourceMutation",
                H.FindText("Name: Bob Updated") is not null);

            person1.Name = "Alice Updated";
            await Harness.Render();

            H.Check("Observable_SourceSwap_OldSourceIgnored",
                H.FindText("Name: Alice Updated") is null);
        }
    }
}
