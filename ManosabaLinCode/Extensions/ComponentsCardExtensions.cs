namespace ManosabaLin.Extensions;

public static class ComponentsCardExtensions
{
    extension(IComponentsCardModel? card)
    {
        public bool HasComponent<T>() where T : class, ICardComponent
        {
            return card?.GetComponent<T>() != null;
        }
    }

    extension(CardModel? card)
    {
        public bool HasComponent<T>() where T : class, ICardComponent
        {
            return card is IComponentsCardModel componentsCardModel && componentsCardModel.GetComponent<T>() != null;
        }

        public ICardComponent? TryAddComponent<T>(T component) where T : class, ICardComponent
        {
            if (card is not IComponentsCardModel componentsCardModel) return null;
            return componentsCardModel.AddComponent(component);
        }

        public ICardComponent? TryRemoveComponent<T>() where T : class, ICardComponent
        {
            if (card is not IComponentsCardModel componentsCardModel) return null;
            return componentsCardModel.RemoveComponent<T>();
        }
    }
}
