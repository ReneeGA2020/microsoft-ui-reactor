using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

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
            TextBlock("Animals List").AutomationId("AnimalsTitle"),
            ListView(Animals,
                keySelector: a => a.Name,
                viewBuilder: (animal, idx) =>
                    HStack(
                        TextBlock($"{idx + 1}.").AutomationId($"AnimalIdx{idx}"),
                        TextBlock(animal.Name).AutomationId($"AnimalName{idx}"),
                        TextBlock($"({animal.Species})").AutomationId($"AnimalSpecies{idx}")
                    )
            ).Height(300).AutomationId("AnimalsList")
        );
}
