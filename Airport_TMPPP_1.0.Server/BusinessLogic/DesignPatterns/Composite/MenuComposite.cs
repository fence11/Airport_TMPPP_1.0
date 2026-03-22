namespace Airport_TMPPP_1._0.Server.BusinessLogic.DesignPatterns.Composite
{
    // ─────────────────────────────────────────────────────────────────────────
    // COMPONENT  –  the common interface for both leaves and composites
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The common component interface that lets client code treat a single
    /// menu item and an entire menu hierarchy identically.
    /// </summary>
    public interface IMenuComponent
    {
        string   Name        { get; }
        string   Description { get; }
        decimal  GetPrice();
        void     Display(int depth = 0);

        // Optional child-management – leaves can leave these as no-ops or
        // throw NotSupportedException, composites implement them fully.
        void Add(IMenuComponent component)    { throw new NotSupportedException(); }
        void Remove(IMenuComponent component) { throw new NotSupportedException(); }
        IEnumerable<IMenuComponent> GetChildren() => Enumerable.Empty<IMenuComponent>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEAF  –  a single food or drink item
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Leaf node: a single product (food or drink) with a fixed price.
    /// Cannot have children.
    /// </summary>
    public sealed class MenuItem : IMenuComponent
    {
        public string  Name        { get; }
        public string  Description { get; }
        public decimal Price       { get; }
        public string  Category    { get; }   // e.g. "Food", "Drink", "Dessert"

        public MenuItem(
            string  name,
            string  description,
            decimal price,
            string  category = "Food")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

            Name        = name;
            Description = description;
            Price       = price;
            Category    = category;
        }

        public decimal GetPrice() => Price;

        public void Display(int depth = 0)
        {
            string indent = new(' ', depth * 2);
            Console.WriteLine(
                $"{indent}• {Name,-30} {Price,7:F2} MDL  [{Category}]  – {Description}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMPOSITE  –  a named menu that contains other components
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Composite node: a named menu section that can contain both
    /// <see cref="MenuItem"/> leaves and nested <see cref="MenuSection"/>
    /// composites.  Implements the same <see cref="IMenuComponent"/> interface
    /// so client code never has to distinguish between the two.
    /// </summary>
    public sealed class MenuSection : IMenuComponent
    {
        private readonly List<IMenuComponent> _children = new();

        public string Name        { get; }
        public string Description { get; }

        public MenuSection(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Section name is required.", nameof(name));

            Name        = name;
            Description = description;
        }

        // ── Child management ────────────────────────────────────────────────

        public void Add(IMenuComponent component)
        {
            if (component is null) throw new ArgumentNullException(nameof(component));
            _children.Add(component);
        }

        public void Remove(IMenuComponent component)
        {
            _children.Remove(component);
        }

        public IEnumerable<IMenuComponent> GetChildren() => _children.AsReadOnly();

        // ── Composite operations ─────────────────────────────────────────────

        /// <summary>
        /// The price of a section is the sum of all its children's prices.
        /// This traverses the whole subtree recursively.
        /// </summary>
        public decimal GetPrice() => _children.Sum(c => c.GetPrice());

        public void Display(int depth = 0)
        {
            string indent = new(' ', depth * 2);
            Console.WriteLine();
            Console.WriteLine(
                $"{indent}▶ {Name}  (total: {GetPrice():F2} MDL)" +
                (string.IsNullOrWhiteSpace(Description) ? "" : $"  – {Description}"));
            Console.WriteLine($"{indent}{new string('─', 50 - depth * 2)}");

            foreach (var child in _children)
                child.Display(depth + 1);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESTAURANT MENU BUILDER  –  demonstration helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Factory that builds the full airport restaurant menu hierarchy.
    /// Demonstrates how the Composite pattern lets leaves and sections
    /// live side-by-side with no special-casing in client code.
    /// </summary>
    public static class AirportRestaurantMenuFactory
    {
        public static MenuSection CreateFullMenu()
        {
            // ── Root menu ───────────────────────────────────────────────────
            var rootMenu = new MenuSection(
                "Airport Bistro",
                "Full menu of Airport Bistro restaurant");

            // ── Breakfast section ────────────────────────────────────────────
            var breakfast = new MenuSection("Breakfast", "Served 06:00 – 10:30");
            breakfast.Add(new MenuItem("Continental Breakfast",
                "Croissant, jam, orange juice, coffee", 85m));
            breakfast.Add(new MenuItem("Eggs Benedict",
                "Poached eggs, hollandaise, ham on an English muffin", 110m));
            breakfast.Add(new MenuItem("Avocado Toast",
                "Sourdough, smashed avocado, cherry tomatoes", 95m, "Food"));

            // Breakfast drinks sub-section
            var breakfastDrinks = new MenuSection("Breakfast Drinks");
            breakfastDrinks.Add(new MenuItem("Freshly Squeezed OJ", "250 ml", 35m, "Drink"));
            breakfastDrinks.Add(new MenuItem("Espresso",            "Single shot", 25m, "Drink"));
            breakfastDrinks.Add(new MenuItem("Cappuccino",          "180 ml",      30m, "Drink"));
            breakfast.Add(breakfastDrinks);

            // ── Daily specials (combo) ──────────────────────────────────────
            var dailySpecials = new MenuSection(
                "Menu of the Day",
                "Changes daily – ask staff for today's options");

            var comboA = new MenuSection("Business Combo A", "Soup + main + water");
            comboA.Add(new MenuItem("Tomato Bisque",     "250 ml soup",         45m));
            comboA.Add(new MenuItem("Grilled Chicken",   "With seasonal vegs", 120m));
            comboA.Add(new MenuItem("Still Water 330ml", "",                    15m, "Drink"));
            dailySpecials.Add(comboA);

            var comboB = new MenuSection("Economy Combo B", "Sandwich + coffee");
            comboB.Add(new MenuItem("Club Sandwich",   "Triple-decker, fries",  75m));
            comboB.Add(new MenuItem("Filter Coffee",   "220 ml, free refill",   20m, "Drink"));
            dailySpecials.Add(comboB);

            // ── Main courses ─────────────────────────────────────────────────
            var mains = new MenuSection("Main Courses", "Served all day");
            mains.Add(new MenuItem("Beef Burger",       "200 g beef patty, brioche bun, fries", 135m));
            mains.Add(new MenuItem("Salmon Fillet",     "With lemon-butter sauce, rice",        165m));
            mains.Add(new MenuItem("Vegetarian Pasta",  "Penne arrabbiata, parmesan",            95m));
            mains.Add(new MenuItem("Caesar Salad",      "Romaine, croutons, anchovies",          85m));

            // ── Desserts ─────────────────────────────────────────────────────
            var desserts = new MenuSection("Desserts & Sweets");
            desserts.Add(new MenuItem("Tiramisu",       "Classic Italian recipe",      65m, "Dessert"));
            desserts.Add(new MenuItem("Chocolate Lava", "Warm cake, vanilla ice cream",70m, "Dessert"));
            desserts.Add(new MenuItem("Fruit Sorbet",   "Three-scoop, seasonal fruit", 55m, "Dessert"));

            // ── Bar & Beverages ──────────────────────────────────────────────
            var bar = new MenuSection("Bar & Beverages");
            bar.Add(new MenuItem("House Wine (glass)", "Red or white, 150 ml",    55m, "Drink"));
            bar.Add(new MenuItem("Craft Beer 500ml",   "Local IPA",               60m, "Drink"));
            bar.Add(new MenuItem("Fresh Lemonade",     "500 ml, mint, ginger",    40m, "Drink"));
            bar.Add(new MenuItem("Soft Drink",         "Can 330 ml",              25m, "Drink"));

            // ── Assemble root ────────────────────────────────────────────────
            rootMenu.Add(breakfast);
            rootMenu.Add(dailySpecials);
            rootMenu.Add(mains);
            rootMenu.Add(desserts);
            rootMenu.Add(bar);

            return rootMenu;
        }
    }
}
