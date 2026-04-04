using Duct;
using Duct.Core;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

internal static class CollectionFixtures
{
    private record Animal(string Name, string Species);

    private static readonly Animal[] Animals =
    [
        new("Rex", "Dog"),
        new("Whiskers", "Cat"),
        new("Polly", "Parrot"),
        new("Nemo", "Fish"),
        new("Buddy", "Dog"),
    ];

    internal static Element ListViewTyped(RenderContext ctx) =>
        VStack(
            Text("Animals List").AutomationId("AnimalsTitle"),
            ListView(Animals,
                keySelector: a => a.Name,
                viewBuilder: (animal, idx) =>
                    HStack(
                        Text($"{idx + 1}.").AutomationId($"AnimalIdx{idx}"),
                        Text(animal.Name).AutomationId($"AnimalName{idx}"),
                        Text($"({animal.Species})").AutomationId($"AnimalSpecies{idx}")
                    )
            ).Height(300).AutomationId("AnimalsList")
        );
}
