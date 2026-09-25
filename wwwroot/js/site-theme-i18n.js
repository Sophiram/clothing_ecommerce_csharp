/**
 * CLOTHÉ - Global Theme (Dark/Light) and Internationalization (ENG/Khmer) Engine
 * Version 2.0 — Full Coverage + OS preference detection
 */
(function () {

    // =========================================================
    //  KHMER DICTIONARY - Complete Site Coverage
    // =========================================================
    const UI_DICTIONARY = {

        // ── Store Front Header & Nav ──
        "Home": "ដើម",
        "Shop": "ហាង",
        "Sale": "បញ្ចុះតម្លៃ",
        "About": "អំពីយើង",
        "Contact": "ទំនាក់ទំនង",
        "Contact Us": "ទំនាក់ទំនងយើង",
        "Get in Touch": "ទំនាក់ទំនងមកកាន់យើង",
        "Telegram Support": "ជំនួយតាម Telegram",
        "Chat on Telegram": "ជជែកតាម Telegram",
        "Dashboard": "ផ្ទាំងគ្រប់គ្រង",
        "Admin Dashboard": "ផ្ទាំងគ្រប់គ្រង",
        "Sign in": "ចូលប្រើ",
        "Login": "ចូលប្រើ",
        "Get started": "ចុះឈ្មោះ",
        "Register": "ចុះឈ្មោះ",
        "Sign Up": "ចុះឈ្មោះ",
        "Account": "គណនី",
        "My Account": "គណនីរបស់ខ្ញុំ",
        "Profile Overview": "ទិដ្ឋភាពទូទៅ",
        "My Orders": "ការបញ្ជាទិញ",
        "Wishlist": "បញ្ជីចូលចិត្ត",
        "Shopping Bag": "កន្ត្រកទំនិញ",
        "Shopping Cart": "កន្ត្រកទំនិញ",
        "Cart": "កន្ត្រក",
        "Sign out": "ចាកចេញ",
        "Logout": "ចាកចេញ",
        "Log out": "ចាកចេញ",
        "View Store": "មើលហាង",
        "Preview Storefront": "មើលហាងផ្ទាល់",

        // ── Hero / Banner ──
        "HOT": "ក្តៅៗ",
        "Flash Sale": "លក់ប្រូម៉ូសិន",
        "Shop Now": "ទិញឥឡូវនេះ",
        "Shop Collection": "ទិញបណ្តុំម៉ូដ",
        "Explore Products": "ស្វែងរកផលិតផល",
        "New Season": "រដូវកាលថ្មី",
        "Free Shipping": "ដឹកជញ្ជូនឥតគិតថ្លៃ",
        "New Arrivals": "ទំនិញចូលថ្មី",
        "Best Sellers": "លក់ដាច់បំផុត",
        "Trending Now": "កំពុងពេញនិយម",

        // ── Shop Page ──
        "Categories": "ប្រភេទ",
        "All Categories": "ប្រភេទទាំងអស់",
        "Popular Brands": "ម៉ាកយីហោពេញនិយម",
        "All Brands": "ម៉ាកយីហោទាំងអស់",
        "Featured Products": "ផលិតផលពិសេស",
        "All Products": "ផលិតផលទាំងអស់",
        "Filters": "តម្រង",
        "Filter": "តម្រង",
        "Sort By": "តម្រៀបតាម",
        "Sort": "តម្រៀប",
        "Price Range": "ជួរតម្លៃ",
        "Min Price": "តម្លៃអប្បបរមា",
        "Max Price": "តម្លៃអតិបរមា",
        "In Stock": "មានស្តុក",
        "Out of Stock": "អស់ស្តុក",
        "On Sale": "បញ្ចុះតម្លៃ",
        "Apply Filters": "អនុវត្តតម្រង",
        "Clear Filters": "សម្អាតតម្រង",
        "Clear All": "សម្អាតទាំងអស់",
        "Showing results for": "លទ្ធផលសម្រាប់",
        "No products found": "រកមិនឃើញផលិតផល",
        "Products found": "ផលិតផលដែលបានរក",
        "Newest": "ថ្មីបំផុត",
        "Oldest": "ចាស់បំផុត",
        "Price: Low to High": "តម្លៃ: ទាបទៅខ្ពស់",
        "Price: High to Low": "តម្លៃ: ខ្ពស់ទៅទាប",
        "Most Popular": "ពេញនិយមបំផុត",
        "Top Rated": "ការវាយតម្លៃខ្ពស់",
        "Previous": "មុន",
        "Next": "បន្ទាប់",
        "Search": "ស្វែងរក",

        // ── Product Card ──
        "Add to Cart": "បន្ថែមទៅកន្ត្រក",
        "Add to Wishlist": "បន្ថែមទៅបញ្ជីចូលចិត្ត",
        "Quick View": "មើលរហ័ស",
        "View Details": "មើលព័ត៌មានលម្អិត",
        "Buy Now": "ទិញឥឡូវ",
        "New": "ថ្មី",
        "Sold Out": "អស់ស្តុក",

        // ── Product Detail Page ──
        "Description": "ការពិពណ៌នា",
        "Product Details": "ព័ត៌មានលម្អិតផលិតផល",
        "Reviews": "ការវាយតម្លៃ",
        "Customer Reviews": "ការវាយតម្លៃអតិថិជន",
        "Write a Review": "សរសេរការវាយតម្លៃ",
        "Related Products": "ផលិតផលពាក់ព័ន្ធ",
        "You may also like": "អ្នកអាចចូលចិត្ត",
        "Size": "ទំហំ",
        "Color": "ពណ៌",
        "Quantity": "បរិមាណ",
        "Availability": "ភាពអាចរក",
        "SKU": "លេខកូដផលិតផល",
        "Brand": "ម៉ាក",
        "Category": "ប្រភេទ",
        "Share": "ចែករំលែក",
        "Rating": "ការវាយតម្លៃ",
        "Verified Purchase": "ការទិញដែលបានផ្ទៀងផ្ទាត់",
        "Helpful": "មានប្រយោជន៍",
        "Report": "រាយការណ៍",
        "30-day return policy": "គោលការណ៍ត្រឡប់ក្នុងរយៈពេល 30 ថ្ងៃ",
        "Secure payment": "ការទូទាត់មានសុវត្ថិភាព",

        // ── Cart Sidebar & Cart Page ──
        "Your cart": "កន្ត្រករបស់អ្នក",
        "Your Cart": "កន្ត្រករបស់អ្នក",
        "items": "ទំនិញ",
        "item": "ទំនិញ",
        "Your cart is empty": "កន្ត្រករបស់អ្នកទទេ",
        "Start shopping": "ចាប់ផ្តើមទិញ",
        "Continue Shopping": "បន្តការទិញ",
        "Remove": "លុប",
        "Subtotal": "សរុបរង",
        "Total": "សរុប",
        "Delivery Fee": "ថ្លៃដឹកជញ្ជូន",
        "Shipping": "ការដឹកជញ្ជូន",
        "Discount": "បញ្ចុះតម្លៃ",
        "Promo Code": "លេខកូដប្រូម៉ូ",
        "Apply": "អនុវត្ត",
        "Free": "ឥតគិតថ្លៃ",
        "Checkout": "ទូទាត់ប្រាក់",
        "Proceed to Checkout": "ទៅដល់ការទូទាត់",
        "Order Summary": "សង្ខេបការបញ្ជាទិញ",
        "Secure Checkout": "ការទូទាត់មានសុវត្ថិភាព",

        // ── Checkout Page ──
        "Delivery Information": "ព័ត៌មានដឹកជញ្ជូន",
        "Delivery Address": "អាសយដ្ឋានដឹកជញ្ជូន",
        "Shipping Address": "អាសយដ្ឋានដឹកជញ្ជូន",
        "Payment Method": "វិធីទូទាត់",
        "Payment": "ការទូទាត់",
        "Full Name": "ឈ្មោះពេញ",
        "Email": "អ៊ីមែល",
        "Email Address": "អ៊ីមែល",
        "Phone": "ទូរស័ព្ទ",
        "Phone Number": "លេខទូរស័ព្ទ",
        "Address": "អាសយដ្ឋាន",
        "City": "ក្រុង",
        "Province": "ខេត្ត",
        "Country": "ប្រទេស",
        "Note": "កំណត់ចំណាំ",
        "Order Note": "កំណត់ចំណាំការបញ្ជាទិញ",
        "Place Order": "បញ្ជាទិញ",
        "Pay Now": "ទូទាត់ឥឡូវនេះ",
        "Bakong KHQR": "បាកុង KHQR",
        "Cash on Delivery": "ទូទាត់ជាសាច់ប្រាក់",
        "Standard Express": "ដឹកជញ្ជូនរហ័ស",
        "Store Pickup": "យកនៅហាង",
        "Other Express": "ក្រុមហ៊ុនដឹកជញ្ជូនផ្សេងទៀត",
        "Delivery Method": "វិធីដឹកជញ្ជូន",
        "1-3 business days": "1-3 ថ្ងៃធ្វើការ",
        "3-5 business days": "3-5 ថ្ងៃធ្វើការ",
        "Same day pickup": "យកថ្ងៃដដែល",
        "Order Confirmation": "ការបញ្ជាក់ការបញ្ជាទិញ",
        "Thank you for your order": "អរគុណសម្រាប់ការបញ្ជាទិញ",

        // ── Orders Page ──
        "Order History": "ប្រវត្តិការបញ្ជាទិញ",
        "Order Date": "កាលបរិច្ឆេទ",
        "Order Status": "ស្ថានភាព",
        "Order Total": "តម្លៃសរុប",
        "Track Order": "តាមដានការបញ្ជាទិញ",
        "View Order": "មើលការបញ្ជាទិញ",
        "Pending": "កំពុងរង់ចាំ",
        "Processing": "កំពុងដំណើរការ",
        "Confirmed": "បានបញ្ជាក់",
        "Shipped": "បានដឹកជញ្ជូន",
        "Delivered": "បានដល់",
        "Cancelled": "បានបោះបង់",
        "Refunded": "បានប្រគល់ប្រាក់",
        "No orders yet": "មិនទាន់មានការបញ្ជាទិញ",

        // ── Profile Page ──
        "Profile": "ប្រវត្តិរូប",
        "My Profile": "ប្រវត្តិរូបរបស់ខ្ញុំ",
        "Personal Information": "ព័ត៌មានផ្ទាល់ខ្លួន",
        "Change Password": "ផ្លាស់ប្តូរពាក្យសម្ងាត់",
        "Security": "សុវត្ថិភាព",
        "Notifications": "ការជូនដំណឹង",
        "Current Password": "ពាក្យសម្ងាត់បច្ចុប្បន្ន",
        "New Password": "ពាក្យសម្ងាត់ថ្មី",
        "Confirm Password": "បញ្ជាក់ពាក្យសម្ងាត់",
        "Update Profile": "ធ្វើបច្ចុប្បន្នភាពប្រវត្តិរូប",
        "Save Changes": "រក្សាទុកការផ្លាស់ប្តូរ",
        "Upload Photo": "បញ្ចូលរូបថត",
        "Member since": "សមាជិកតាំងពី",

        // ── Reviews Page ──
        "Leave a Review": "ដាក់ការវាយតម្លៃ",
        "Submit Review": "ដាក់ការវាយតម្លៃ",
        "Your Rating": "ការវាយតម្លៃរបស់អ្នក",
        "Your Review": "ការវាយតម្លៃរបស់អ្នក",
        "Excellent": "ល្អឥតខ្ចោះ",
        "Good": "ល្អ",
        "Average": "មធ្យម",
        "Poor": "មិនល្អ",

        // ── Footer ──
        "Quick Links": "តំណភ្ជាប់រហ័ស",
        "Customer Service": "សេវាអតិថិជន",
        "Help Center": "មជ្ឈមណ្ឌលជំនួយ",
        "FAQ": "សំណួរញឹកញាប់",
        "Contact Us": "ទំនាក់ទំនងយើង",
        "About Us": "អំពីយើង",
        "Our Story": "រឿងរ៉ាវរបស់យើង",
        "Shipping Policy": "គោលការណ៍ដឹកជញ្ជូន",
        "Return Policy": "គោលការណ៍ត្រឡប់",
        "Privacy Policy": "គោលការណ៍ឯកជនភាព",
        "Terms of Service": "លក្ខខណ្ឌ",
        "Terms & Conditions": "លក្ខខណ្ឌ",
        "Newsletter": "ព្រឹត្តិបត្រ",
        "Subscribe": "ជាវ",
        "Follow us": "តាមដានយើង",
        "All rights reserved": "រក្សាសិទ្ធគ្រប់យ៉ាង",
        "in Cambodia": "នៅកម្ពុជា",

        // ── Common Buttons & Actions ──
        "Clear": "សម្អាត",
        "Save": "រក្សាទុក",
        "Cancel": "បោះបង់",
        "Edit": "កែប្រែ",
        "Delete": "លុប",
        "Add New": "បន្ថែមថ្មី",
        "Create": "បង្កើត",
        "Publish Changes": "បោះពុម្ពការផ្លាស់ប្តូរ",
        "Save Homepage Settings": "រក្សាទុកការកំណត់ទំព័រដើម",
        "Submit": "ដាក់ស្នើ",
        "Back": "ត្រឡប់",
        "Close": "បិទ",
        "Confirm": "បញ្ជាក់",
        "Loading...": "កំពុងផ្ទុក...",
        "View All": "មើលទាំងអស់",
        "See All": "មើលទាំងអស់",
        "Show More": "បង្ហាញបន្ថែម",
        "Read More": "អានបន្ថែម",

        // ── Admin Sidebar & Menu ──
        "MAIN": "មេ",
        "CATALOG": "កាតាឡុក",
        "Homepage & Hero": "ទំព័រដើម & ផ្ទាំងផ្សាយ",
        "Products": "ផលិតផល",
        "Categories": "ប្រភេទ",
        "Brands": "ម៉ាកយីហោ",
        "Sizes": "ទំហំ",
        "Colors": "ពណ៌",
        "Product Variants": "ជម្រើសផលិតផល",
        "Inventory": "ស្តុកទំនិញ",
        "Inventories": "ស្តុកទំនិញ",
        "SALES": "ការលក់",
        "SALES & FULFILLMENT": "ការលក់ & ការដឹកជញ្ជូន",
        "SALES / PAYMENTS": "ការលក់ & ការទូទាត់",
        "SALES & TRANSACTIONS": "ការលក់ & ប្រតិបត្តិការ",
        "Orders": "ការបញ្ជាទិញ",
        "Payments": "ការទូទាត់",
        "Payment Methods": "វិធីសាស្ត្រទូទាត់",
        "Shipments": "ការដឹកជញ្ជូន",
        "Delivery": "ដឹកជញ្ជូន",
        "Delivery Methods": "វិធីដឹកជញ្ជូន",
        "Delivery Methods & Logistics": "វិធីដឹកជញ្ជូន & ភស្តុភារ",
        "Delivery Methods & Pricing": "វិធីដឹកជញ្ជូន & តម្លៃ",
        "Delivery Branches": "សាខាដឹកជញ្ជូន",
        "VET Express Branches": "សាខា VET Express",
        "VET Express Pickup Branches": "សាខាទទួលទំនិញ VET Express",
        "Telegram": "តេឡេក្រាម",
        "Telegram Bot": "Telegram Bot",
        "Telegram Bot Notification": "ការជូនដំណឹង Telegram Bot",
        "Telegram Bot Integration": "ការតភ្ជាប់ Telegram Bot",
        "Telegram Settings": "ការកំណត់ Telegram",
        "Mail Service": "សេវាអ៊ីមែល",
        "SMTP Mail Service": "សេវាអ៊ីមែល SMTP",
        "SMTP Mail Service & Notifications": "សេវាអ៊ីមែល SMTP & ការជូនដំណឹង",
        "Dispatch Test Email": "ផ្ញើសារសាកល្បង",
        "CUSTOMERS": "អតិថិជន",
        "CUSTOMERS & SOCIAL PROOF": "អតិថិជន & ការវាយតម្លៃ",
        "CUSTOMERS & ENGAGEMENT": "អតិថិជន & ការចូលចិត្ត",
        "Customers": "អតិថិជន",
        "Customer Management": "ការគ្រប់គ្រងអតិថិជន",
        "Reviews": "ការវាយតម្លៃ",
        "Customer Reviews": "ការវាយតម្លៃអតិថិជន",
        "Wishlists": "បញ្ជីចូលចិត្ត",
        "Customer Wishlists": "បញ្ជីចូលចិត្តរបស់អតិថិជន",
        "SYSTEM": "ប្រព័ន្ធ",
        "Users": "អ្នកប្រើប្រាស់",
        "Admins": "អ្នកគ្រប់គ្រង",
        "Roles": "តួនាទី",
        "Settings": "ការកំណត់",
        "Audit Logs": "កំណត់ហេតុសវនកម្ម",
        "Audit Log": "កំណត់ហេតុប្រព័ន្ធ",
        "STORE": "ហាង",
        "View Store": "មើលហាង",
        "Storefront Customization": "ការកែសម្រួលទំព័រដើម",
        "Manage Homepage & Hero": "គ្រប់គ្រងទំព័រដើម & បដា Hero",
        "Manage Homepage & Hero Banner": "គ្រប់គ្រងទំព័រដើម & បដា Hero",

        // ── Admin Subtitles & Descriptions ──
        "Organize your products into categories.": "រៀបចំផលិតផលរបស់អ្នកតាមប្រភេទនីមួយៗ។",
        "Manage your clothing products, pricing, and availability.": "គ្រប់គ្រងផលិតផលសម្លៀកបំពាក់ តម្លៃ និងភាពអាចរកបាន។",
        "Manage product brands and their logos.": "គ្រប់គ្រងម៉ាកយីហោផលិតផល និងរូបសញ្ញា។",
        "Manage product sizes (e.g., S, M, L, XL).": "គ្រប់គ្រងទំហំផលិតផល (ឧ. S, M, L, XL)។",
        "Manage product colors.": "គ្រប់គ្រងពណ៌ផលិតផល។",
        "Manage product size, color, price and inventory.": "គ្រប់គ្រងទំហំ ពណ៌ តម្លៃ និងស្តុកទំនិញ។",
        "Cross-product stock levels — a warehouse view across every variant.": "កម្រិតស្តុកផលិតផល — ទិដ្ឋភាពឃ្លាំងសម្រាប់គ្រប់ជម្រើស។",
        "Manage customer purchases, track fulfillment progression, and update statuses.": "គ្រប់គ្រងការទិញរបស់អតិថិជន តាមដានការដឹកជញ្ជូន និងធ្វើបច្ចុប្បន្នភាពស្ថានភាព។",
        "Monitor financial transactions, gateway reconciliation, and revenue settlements.": "តាមដានប្រតិបត្តិការហិរញ្ញវត្ថុ ការទូទាត់ និងការផ្សះផ្សាចំណូល។",
        "Manage payment methods available during checkout.": "គ្រប់គ្រងវិធីសាស្ត្រទូទាត់ដែលអាចប្រើបាននៅពេលទូទាត់ប្រាក់។",
        "Track parcels, carriers, and dispatch status across customer orders.": "តាមដានកញ្ចប់ទំនិញ ក្រុមហ៊ុនដឹកជញ្ជូន និងស្ថានភាពបញ្ជូន។",
        "Manage checkout delivery options, fees, and VET Express branch pickup locations.": "គ្រប់គ្រងជម្រើសដឹកជញ្ជូន តម្លៃ និងទីតាំងសាខា VET Express។",
        "Configure provinces and branch office locations available for customer pickup selection.": "កំណត់រចនាសម្ព័ន្ធខេត្ត និងសាខាដែលមានសម្រាប់អតិថិជនជ្រើសរើសមកយក។",
        "Configure your Telegram Bot to receive real-time order alerts and store notifications.": "កំណត់រចនាសម្ព័ន្ធ Telegram Bot ដើម្បីទទួលបានការជូនដំណឹងការបញ្ជាទិញភ្លាមៗ។",
        "Browse and manage customer profiles, status, orders, and interaction history.": "រកមើល និងគ្រប់គ្រងគណនីអតិថិជន ស្ថានភាព ការបញ្ជាទិញ និងប្រវត្តិ។",
        "Moderate, inspect, and analyze customer feedback across all clothing items.": "ត្រួតពិនិត្យ និងវិភាគមតិកែលម្អរបស់អតិថិជនលើគ្រប់ទំនិញ។",
        "Inspect customer demand, saved favorites, and high-interest products.": "ពិនិត្យមើលតម្រូវការអតិថិជន ទំនិញដែលបានរក្សាទុក និងផលិតផលពេញនិយម។",

        // ── Admin Card Headers ──
        "All Categories": "ប្រភេទទាំងអស់",
        "Search and manage categories": "ស្វែងរក និងគ្រប់គ្រងប្រភេទ",
        "All Brands": "ម៉ាកយីហោទាំងអស់",
        "Search and manage brands": "ស្វែងរក និងគ្រប់គ្រងម៉ាកយីហោ",
        "All Sizes": "ទំហំទាំងអស់",
        "Manage sizes used across product variants": "គ្រប់គ្រងទំហំដែលប្រើសម្រាប់ជម្រើសផលិតផល",
        "All Colors": "ពណ៌ទាំងអស់",
        "Manage colors used across product variants": "គ្រប់គ្រងពណ៌ដែលប្រើសម្រាប់ជម្រើសផលិតផល",
        "Variant List": "បញ្ជីជម្រើសផលិតផល",
        "All configured size/color combinations": "បន្សំទំហំ និងពណ៌ដែលបានកំណត់រចនាសម្ព័ន្ធទាំងអស់",
        "All Inventory Records": "កំណត់ត្រាស្តុកទាំងអស់",
        "Stock, reserved units, and last update per SKU": "ស្តុក ចំនួនបម្រុងទុក និងបច្ចុប្បន្នភាពចុងក្រោយតាម SKU",
        "All Products": "ផលិតផលទាំងអស់",
        "Filter and manage your product catalog": "តម្រង និងគ្រប់គ្រងកាតាឡុកផលិតផលរបស់អ្នក",
        "All Shipments": "ការដឹកជញ្ជូនទាំងអស់",
        "Real-time delivery progress and tracking codes": "វឌ្ឍនភាពដឹកជញ្ជូនជាក់ស្តែង និងលេខកូដតាមដាន",
        "All Customer Reviews": "ការវាយតម្លៃអតិថិជនទាំងអស់",
        "Customer ratings and comments": "ការវាយតម្លៃ និងមតិយោបល់របស់អតិថិជន",
        "All Customer Wishlists": "បញ្ជីចូលចិត្តអតិថិជនទាំងអស់",
        "Wishlists grouped by customer": "បញ្ជីចូលចិត្តចាត់ជាក្រុមតាមអតិថិជន",

        // ── Admin Action Buttons & Modal Titles ──
        "Add Category": "បន្ថែមប្រភេទ",
        "Add Product": "បន្ថែមផលិតផល",
        "Add Brand": "បន្ថែមម៉ាកយីហោ",
        "Add Size": "បន្ថែមទំហំ",
        "Add Color": "បន្ថែមពណ៌",
        "Add Variant": "បន្ថែមជម្រើស",
        "Add Inventory": "បន្ថែមស្តុក",
        "Add Payment Method": "បន្ថែមវិធីទូទាត់",
        "Add New Branch": "បន្ថែមសាខាថ្មី",
        "Record Payment": "កត់ត្រាការទូទាត់",
        "Create Order": "បង្កើតការបញ្ជាទិញ",
        "Create Product": "បង្កើតផលិតផល",
        "Create Category": "បង្កើតប្រភេទ",
        "Create Brand": "បង្កើតម៉ាកយីហោ",
        "Create Variant": "បង្កើតជម្រើស",
        "Edit Product": "កែប្រែផលិតផល",
        "Edit Category": "កែប្រែប្រភេទ",
        "Edit Brand": "កែប្រែម៉ាកយីហោ",
        "Edit Size": "កែប្រែទំហំ",
        "Edit Color": "កែប្រែពណ៌",
        "Edit Variant": "កែប្រែជម្រើស",
        "Edit Stock": "កែប្រែស្តុក",
        "Edit Method": "កែប្រែវិធីទូទាត់",
        "Edit Profile": "កែប្រែប្រវត្តិរូប",
        "Delete Product": "លុបផលិតផល",
        "Delete Category": "លុបប្រភេទ",
        "Delete Brand": "លុបម៉ាកយីហោ",
        "Delete Size": "លុបទំហំ",
        "Delete Color": "លុបពណ៌",
        "Delete Variant": "លុបជម្រើស",
        "Delete Inventory": "លុបស្តុក",
        "Delete Order": "លុបការបញ្ជាទិញ",
        "Delete Method": "លុបវិធីទូទាត់",
        "Delete Review": "លុបការវាយតម្លៃ",
        "Delete Shipment": "លុបការដឹកជញ្ជូន",
        "Save Changes": "រក្សាទុកការកែប្រែ",
        "Update Status": "ធ្វើបច្ចុប្បន្នភាពស្ថានភាព",
        "Toggle Status": "ប្តូរស្ថានភាព",
        "Disable Method": "បិទវិធីទូទាត់",
        "Enable Method": "បើកវិធីទូទាត់",
        "View Review": "មើលការវាយតម្លៃ",
        "View Wishlist Details": "មើលបញ្ជីចូលចិត្ត",
        "Reset All Filters": "កំណត់តម្រងទាំងអស់ឡើងវិញ",
        "Reset Filters": "កំណត់តម្រងឡើងវិញ",

        // ── Admin Table Headers & Columns ──
        "Product Name": "ឈ្មោះផលិតផល",
        "Size Name": "ឈ្មោះទំហំ",
        "Color Name": "ឈ្មោះពណ៌",
        "Variants Linked": "ជម្រើសដែលភ្ជាប់",
        "Hex Code / Preview": "កូដ Hex / មើលគំរូ",
        "SKU": "SKU",
        "Gender": "ភេទ",
        "Material": "សម្ភារៈ",
        "Variants": "ជម្រើស",
        "Stock": "ស្តុក",
        "Available": "អាចប្រើបាន",
        "Reserved": "បានបម្រុង",
        "Updated": "បានធ្វើបច្ចុប្បន្នភាព",
        "Customer": "អតិថិជន",
        "Order": "ការបញ្ជាទិញ",
        "Items": "ទំនិញ",
        "Total Amount": "ចំនួនទឹកប្រាក់សរុប",
        "Tracking Number": "លេខតាមដាន",
        "Carrier": "ក្រុមហ៊ុនដឹកជញ្ជូន",
        "Dispatch Date": "កាលបរិច្ឆេទបញ្ជូន",
        "Estimated Delivery": "ការដឹកជញ្ជូនប៉ាន់ស្មាន",
        "Delivered At": "បានដល់នៅ",
        "Registered": "បានចុះឈ្មោះ",
        "Saved Items": "ទំនិញរក្សាទុក",
        "total": "សរុប",
        "records": "កំណត់ត្រា",
        "wishlists": "បញ្ជី",

        // ── Admin Form Dropdowns & Filters ──
        "All Products": "ផលិតផលទាំងអស់",
        "All Categories": "ប្រភេទទាំងអស់",
        "All Brands": "ម៉ាកយីហោទាំងអស់",
        "All Status": "ស្ថានភាពទាំងអស់",
        "Select category": "ជ្រើសរើសប្រភេទ",
        "Select brand": "ជ្រើសរើសម៉ាកយីហោ",
        "Select gender": "ជ្រើសរើសភេទ",
        "Select New Status": "ជ្រើសរើសស្ថានភាពថ្មី",
        "Men": "បុរស",
        "Women": "ស្ត្រី",
        "Unisex": "ទាំងពីរភេទ (Unisex)",
        "Kids": "កុមារ",
        "Active": "សកម្ម",
        "Inactive": "អសកម្ម",
        "Disabled": "បានបិទ",
        "Blocked": "បានរារាំង",
        "Draft": "ព្រាង",
        "Archived": "បានរក្សាទុក",
        "Pending": "កំពុងរង់ចាំ",
        "Processing": "កំពុងដំណើរការ",
        "Confirmed": "បានបញ្ជាក់",
        "Shipped": "បានផ្ញើចេញ",
        "Delivered": "បានដឹកជញ្ជូនដល់",
        "Cancelled": "បានបោះបង់",
        "Refunded": "បានសងប្រាក់វិញ",
        "Paid": "បានទូទាត់",
        "Unpaid": "មិនទាន់ទូទាត់",
        "Failed": "បរាជ័យ",

        // ── Search Placeholders ──
        "Search categories...": "ស្វែងរកប្រភេទ...",
        "Search brands...": "ស្វែងរកម៉ាកយីហោ...",
        "Search product name or description...": "ស្វែងរកឈ្មោះផលិតផល ឬការពិពណ៌នា...",
        "Search by name, email, phone...": "ស្វែងរកតាមឈ្មោះ អ៊ីមែល ទូរស័ព្ទ...",
        "Search customer name or email...": "ស្វែងរកឈ្មោះអតិថិជន ឬអ៊ីមែល...",
        "Search styles, brands, collections…": "ស្វែងរកម៉ូដ ម៉ាកយីហោ បណ្តុំ...",

        // ── System & Admin Details ──
        "Are you sure?": "តើអ្នកប្រាកដទេ?",
        "This action cannot be undone.": "សកម្មភាពនេះមិនអាចត្រឡប់វិញបានទេ។",
        "Admin Management System": "ប្រព័ន្ធគ្រប់គ្រងរដ្ឋបាល",

        // ── Admin Dashboard Hero & Metric KPI Cards ──
        "Platform Online": "ប្រព័ន្ធដំណើរការ",
        "Store Administrator": "អ្នកគ្រប់គ្រងហាង",
        "Enterprise Administrator": "អ្នកគ្រប់គ្រងសហគ្រាស",
        "Super Admin": "រដ្ឋបាលជាន់ខ្ពស់",
        "Store Admin": "រដ្ឋបាលហាង",
        "Super Admin Dashboard": "ផ្ទាំងគ្រប់គ្រងរដ្ឋបាលជាន់ខ្ពស់",
        "Store Admin Dashboard": "ផ្ទាំងគ្រប់គ្រងរដ្ឋបាលហាង",
        "Storefront": "ទំព័រហាង",
        "Refresh": "ផ្ទុកឡើងវិញ",
        "STORE REVENUE": "ចំណូលហាង",
        "Store Revenue": "ចំណូលហាង",
        "TODAY'S SALES": "ការលក់ថ្ងៃនេះ",
        "Today's Sales": "ការលក់ថ្ងៃនេះ",
        "ORDERS TO FULFILL": "ការបញ្ជាទិញត្រូវរៀបចំ",
        "Orders To Fulfill": "ការបញ្ជាទិញត្រូវរៀបចំ",
        "INVENTORY ALERTS": "ការដាស់តឿនស្តុក",
        "Inventory Alerts": "ការដាស់តឿនស្តុក",
        "7-Day Store Sales Velocity": "ល្បឿនលក់ប្រចាំ ៧ ថ្ងៃ",
        "Order Fulfillment Donut": "ស្ថានភាពបំពេញការបញ្ជាទិញ",
        "Order Status Distribution": "ការបែងចែកស្ថានភាពបញ្ជាទិញ",
        "Revenue trend ($) and daily order transactions": "និន្នាការចំណូល ($) និងប្រតិបត្តិការប្រចាំថ្ងៃ",
        "Status of customer purchases": "ស្ថានភាពនៃការទិញរបស់អតិថិជន",
        "Fulfillment pipeline breakdown across all orders": "ការវិភាគលំហូរការងារដឹកជញ្ជូនលើការបញ្ជាទិញទាំងអស់",
        "User Roles & Access": "តួនាទីអ្នកប្រើប្រាស់ & ការចូលប្រើ",
        "RBAC platform account segregation": "ការបែងចែកគណនីប្រព័ន្ធ RBAC",
        "Catalog by Category": "កាតាឡុកតាមប្រភេទ",
        "Product distribution across retail categories": "ការបែងចែកផលិតផលតាមប្រភេទលក់រាយ",
        "System & Governance": "ប្រព័ន្ធ & អភិបាលកិច្ច",
        "Security administration shortcuts": "ផ្លូវកាត់ការគ្រប់គ្រងសុវត្ថិភាព",
        "Administrator Accounts": "គណនីអ្នកគ្រប់គ្រង",
        "Manage platform credentials and lockouts": "គ្រប់គ្រងព័ត៌មានសម្ងាត់ និងការចាក់សោប្រព័ន្ធ",
        "Recent Orders": "ការបញ្ជាទិញថ្មីៗ",
        "Latest customer purchases across the platform": "ការទិញចុងក្រោយរបស់អតិថិជននៅលើប្រព័ន្ធ",
        "Attention Required: Low Stock & Depleted Variants": "ត្រូវការយកចិត្តទុកដាក់: ស្តុកទាប & ជម្រើសអស់ស្តុក",
        "Inventory Manager": "អ្នកគ្រប់គ្រងស្តុក",
        "Restock": "បញ្ចូលស្តុក",
        "Live Stream": "ទិន្នន័យផ្ទាល់",
        "orders today": "ការបញ្ជាទិញថ្ងៃនេះ",
        "out of stock": "អស់ពីស្តុក",
        "in prep": "កំពុងរៀបចំ",
        "pending": "កំពុងរង់ចាំ",
        "units left": "ឯកតានៅសល់",
        "Monthly:": "ប្រចាំខែ:",
        "Total:": "សរុប:",
        "Order ID": "លេខបញ្ជាទិញ",
        "Order Status": "ស្ថានភាពបញ្ជាទិញ",
        "View All Orders": "មើលការបញ្ជាទិញទាំងអស់",
        "Order Details": "ព័ត៌មានលម្អិតបញ្ជាទិញ",
        "View Details": "មើលព័ត៌មានលម្អិត",
        // ── Invoices, Receipts & Orders Extended ──
        "Invoices & Receipts": "វិក្កយបត្រ & បង្កាន់ដៃ",
        "Invoices": "វិក្កយបត្រ",
        "Receipts": "បង្កាន់ដៃ",
        "Receipt": "បង្កាន់ដៃ",
        "Invoice": "វិក្កយបត្រ",
        "Tax Invoice": "វិក្កយបត្រពន្ធ",
        "Official Tax Receipt": "បង្កាន់ដៃពន្ធផ្លូវការ",
        "Print Receipt": "បោះពុម្ពបង្កាន់ដៃ",
        "Print Invoice": "បោះពុម្ពវិក្កយបត្រ",
        "Print Tax Invoice": "បោះពុម្ពវិក្កយបត្រពន្ធ",
        "Digital Receipt": "បង្កាន់ដៃអេឡិចត្រូនិក",
        "Print / View Receipt": "បោះពុម្ព / មើលបង្កាន់ដៃ",
        "Print / Receipt": "បោះពុម្ព / បង្កាន់ដៃ",
        "Back to Orders": "ត្រឡប់ទៅការបញ្ជាទិញ",
        "Back to Order Details": "ត្រឡប់ទៅព័ត៌មានលម្អិត",
        "Order Confirmed": "ការបញ្ជាទិញបានជោគជ័យ",
        "Order Confirmed!": "ការបញ្ជាទិញទទួលបានជោគជ័យ!",
        "Order Details": "ព័ត៌មានលម្អិតបញ្ជាទិញ",
        "Order Summary": "សង្ខេបការបញ្ជាទិញ",
        "Order Reference": "លេខកូដបញ្ជាទិញ",
        "Payment Status": "ស្ថានភាពទូទាត់",
        "Payment Method": "វិធីសាស្ត្រទូទាត់",
        "Payment Methods": "វិធីសាស្ត្រទូទាត់",
        "Delivery Method": "វិធីសាស្ត្រដឹកជញ្ជូន",
        "Delivery Methods": "វិធីសាស្ត្រដឹកជញ្ជូន",
        "Delivery Address": "អាសយដ្ឋានដឹកជញ្ជូន",
        "Shipping Address": "អាសយដ្ឋានដឹកជញ្ជូន",
        "Billing Address": "អាសយដ្ឋានទូទាត់",
        "Shipping & Handling": "ថ្លៃដឹកជញ្ជូន & វេចខ្ចប់",
        "Items Subtotal": "សរុបរងទំនិញ",
        "Items Subtotal:": "សរុបរងទំនិញ:",
        "Grand Total (USD)": "សរុបចុងក្រោយ (ដុល្លារ)",
        "Grand Total (USD):": "សរុបចុងក្រោយ (ដុល្លារ):",
        "Equivalent in KHR": "សមមូលជាប្រាក់រៀល",
        "Equivalent in KHR:": "សមមូលជាប្រាក់រៀល:",
        "Total Paid": "បានទូទាត់សរុប",
        "Paid & Verified": "បានទូទាត់ & ផ្ទៀងផ្ទាត់",
        "PAID & VERIFIED": "បានទូទាត់ & ផ្ទៀងផ្ទាត់",
        "PAID": "បានទូទាត់",
        "Paid": "បានទូទាត់",
        "UNPAID": "មិនទាន់ទូទាត់",
        "Unpaid": "មិនទាន់ទូទាត់",
        "Payment Pending": "កំពុងរង់ចាំការទូទាត់",
        "Payment Successful!": "ការទូទាត់ទទួលបានជោគជ័យ!",
        "Payment Verified!": "ការទូទាត់ត្រូវបានផ្ទៀងផ្ទាត់!",
        "Bakong KHQR Confirmed": "បានផ្ទៀងផ្ទាត់ Bakong KHQR",
        "Waiting for mobile scan… Auto-submits when paid!": "កំពុងរង់ចាំការស្កេនទូរស័ព្ទ... បញ្ជូនដោយស្វ័យប្រវត្តិពេលទូទាត់រួច!",
        "Waiting for payment scan…": "កំពុងរង់ចាំការស្កេនទូទាត់...",
        "Simulate Real Payment (Auto-Paid)": "⚡ សាកល្បងទូទាត់ជាក់ស្តែង (ស្វ័យប្រវត្តិ)",
        "⚡ Simulate Real Payment (Auto-Paid)": "⚡ សាកល្បងទូទាត់ជាក់ស្តែង (ស្វ័យប្រវត្តិ)",
        "Manual Confirm Payment": "បញ្ជាក់ការទូទាត់ដោយដៃ",
        "Save QR": "រក្សាទុក QR",
        "Copy QR": "ចម្លង QR",
        "Open ABA": "បើក ABA",
        "Purchased Items": "ទំនិញដែលបានទិញ",
        "Item Description": "ការពិពណ៌នាទំនិញ",
        "Unit Price": "តម្លៃរាយ",
        "Qty": "បរិមាណ",
        "Line Total": "សរុបបន្ទាត់",
        "Subtotal": "សរុបរង",
        "Total": "សរុប",
        "Carrier": "ក្រុមហ៊ុនដឹក",
        "Carrier:": "ក្រុមហ៊ុនដឹក:",
        "Tracking #": "លេខតាមដាន #",
        "Tracking #:": "លេខតាមដាន #:",
        "Tracking Number": "លេខកូដតាមដាន",
        "Shipment": "ការដឹកជញ្ជូន",
        "Shipments": "ការដឹកជញ្ជូនទាំងអស់",
        "Shipment Status": "ស្ថានភាពដឹកជញ្ជូន",
        "Customer Profile": "ព័ត៌មានអតិថិជន",
        "Customer Name": "ឈ្មោះអតិថិជន",
        "Customer": "អតិថិជន",
        "Customers": "អតិថិជនទាំងអស់",
        "All Statuses": "ស្ថានភាពទាំងអស់",
        "All Payment Statuses": "ស្ថានភាពទូទាត់ទាំងអស់",
        "Search": "ស្វែងរក",
        "Reset": "កំណត់ឡើងវិញ",
        "Filter": "តម្រង",
        "Update Status": "កែប្រែស្ថានភាព",
        "Delete Order": "លុបការបញ្ជាទិញ",
        "Continue Shopping": "បន្តការទិញទំនិញ",
        "View Store": "មើលហាង",
        "Preview Storefront": "មើលហាងផ្ទាល់",
        "Financial Reports": "របាយការណ៍ហិរញ្ញវត្ថុ",
        "Products": "ផលិតផលទាំងអស់",
        "Categories": "ប្រភេទទាំងអស់",
        "Brands": "ម៉ាកយីហោទាំងអស់",
        "Sizes": "ទំហំទាំងអស់",
        "Colors": "ពណ៌ទាំងអស់",
        "Product Variants": "ជម្រើសផលិតផល",
        "Inventory": "ការគ្រប់គ្រងស្តុក",
        "Inventories": "ស្តុកទំនិញ",
        "Orders": "ការបញ្ជាទិញទាំងអស់",
        "Payments": "ការទូទាត់ទាំងអស់",
        "Telegram Bot": "តេឡេក្រាមបូត",
        "Administrators": "អ្នកគ្រប់គ្រងប្រព័ន្ធ",
        "System Settings": "ការកំណត់ប្រព័ន្ធ",
        "Home Settings": "ការកំណត់ទំព័រដើម",
        "Mail Service": "សេវាអ៊ីមែល",
        "Audit Logs": "កំណត់ហេតុសវនកម្ម",
        "Daily retail performance, fulfillment workflow, inventory stock distribution, and product analytics.": "ដំណើរការលក់រាយប្រចាំថ្ងៃ លំហូរការងារដឹកជញ្ជូន ការបែងចែកស្តុក និងការវិភាគផលិតផល។",
        "Real-time enterprise metrics, financial performance, role access controls, and security audit trail.": "ម៉ែត្រិកសហគ្រាសជាក់ស្តែង ដំណើរការហិរញ្ញវត្ថុ ការគ្រប់គ្រងការចូលប្រើ និងកំណត់ហេតុសវនកម្ម។"
    };

    // =========================================================
    //  THEME ENGINE
    // =========================================================

    function getStoredTheme() {
        const stored = localStorage.getItem('site_theme');
        if (stored) return stored;
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    function getStoredLang() {
        return localStorage.getItem('site_language') || 'en';
    }

    function applyTheme(theme) {
        const isDark = theme === 'dark';
        document.documentElement.classList.toggle('dark', isDark);
        document.documentElement.classList.toggle('dark-mode', isDark);
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
        if (document.body) {
            document.body.classList.toggle('dark-mode', isDark);
            document.body.setAttribute('data-theme', isDark ? 'dark' : 'light');
        }
        updateThemeControlsUI(theme);
    }

    function updateThemeControlsUI(theme) {
        const isDark = theme === 'dark';
        document.querySelectorAll('#globalThemeIcon, #adminThemeIcon, #mobileThemeIcon, #themeIcon').forEach(icon => {
            icon.className = isDark ? 'bi bi-sun-fill text-warning' : 'bi bi-moon-stars-fill';
        });
        document.querySelectorAll('#globalThemeLabel, #adminThemeLabel, #mobileThemeLabel, #themeLabel').forEach(lbl => {
            lbl.textContent = isDark ? 'Light' : 'Dark';
        });
    }

    // =========================================================
    //  LANGUAGE ENGINE (UNIVERSAL DOM TRANSLATOR)
    // =========================================================

    function applyKhmerFontStyles(isKh) {
        let styleEl = document.getElementById('clothe-khmer-font-override');
        if (isKh) {
            if (!styleEl) {
                styleEl = document.createElement('style');
                styleEl.id = 'clothe-khmer-font-override';
                styleEl.textContent = `
                    html[lang="km"], html.lang-kh, html.lang-kh body, html.lang-kh button, html.lang-kh input, html.lang-kh select, html.lang-kh textarea, html.lang-kh h1, html.lang-kh h2, html.lang-kh h3, html.lang-kh h4, html.lang-kh h5, html.lang-kh h6 {
                        font-family: 'Kantumruy Pro', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
                    }
                `;
                document.head.appendChild(styleEl);
            }
        } else if (styleEl) {
            styleEl.remove();
        }
    }

    function updateLangControlsUI(lang) {
        const isKh = lang === 'kh';
        const flagSrc = isKh ? '/images/flags/kh.svg' : '/images/flags/gb.svg';
        const flagAlt = isKh ? 'ភាសាខ្មែរ' : 'English';
        document.querySelectorAll('#globalLangFlag, #adminLangFlag, #mobileLangFlag').forEach(f => {
            f.innerHTML = `<img src="${flagSrc}" alt="${flagAlt}" style="width: 20px; height: 14px; object-fit: cover; border-radius: 2px; vertical-align: middle; display: inline-block; box-shadow: 0 1px 2px rgba(0,0,0,0.18);" />`;
        });
        document.querySelectorAll('#globalLangText, #adminLangText, #mobileLangText').forEach(t => t.textContent = isKh ? 'ខ្មែរ' : 'ENG');
        document.querySelectorAll('#checkLangEn, #adminCheckLangEn, #mobileCheckLangEn').forEach(c => {
            c.classList.toggle('hidden', isKh);
            c.classList.toggle('d-none', isKh);
        });
        document.querySelectorAll('#checkLangKh, #adminCheckLangKh, #mobileCheckLangKh').forEach(c => {
            c.classList.toggle('hidden', !isKh);
            c.classList.toggle('d-none', !isKh);
        });
        
        document.documentElement.setAttribute('lang', isKh ? 'km' : 'en');
        document.documentElement.classList.toggle('lang-kh', isKh);
        applyKhmerFontStyles(isKh);
    }

    function translateDynamicText(text, isKhmer) {
        if (!text) return text;
        if (!isKhmer) return text;
        
        const trimmed = text.trim();
        if (UI_DICTIONARY[trimmed]) return UI_DICTIONARY[trimmed];

        // Case-insensitive lookup
        const lowerKey = trimmed.toLowerCase();
        for (const [k, v] of Object.entries(UI_DICTIONARY)) {
            if (k.toLowerCase() === lowerKey) return v;
        }

        // Dynamic patterns
        if (lowerKey.startsWith('welcome back')) {
            const user = trimmed.replace(/^welcome back,?\s*/i, '');
            return `សូមស្វាគមន៍ការត្រឡប់មកវិញ, ${user}`;
        }
        if (/\d+\s+orders today/i.test(trimmed)) {
            return trimmed.replace(/(\d+)\s+orders today/i, '$1 ការបញ្ជាទិញថ្ងៃនេះ');
        }
        if (/\d+\s+pending/i.test(trimmed)) {
            return trimmed.replace(/(\d+)\s+pending/i, '$1 កំពុងរង់ចាំ');
        }
        if (/\d+\s+in prep/i.test(trimmed)) {
            return trimmed.replace(/(\d+)\s+in prep/i, '$1 កំពុងរៀបចំ');
        }
        if (/\d+\s+out of stock/i.test(trimmed)) {
            return trimmed.replace(/(\d+)\s+out of stock/i, '$1 អស់ពីស្តុក');
        }
        if (/\d+\s+units left/i.test(trimmed)) {
            return trimmed.replace(/(\d+)\s+units left/i, 'នៅសល់ $1 ឯកតា');
        }

        return text;
    }

    function translatePage(lang) {
        const isKh = lang === 'kh';

        // 1. Explicit data-en / data-kh attributes (highest priority)
        document.querySelectorAll('[data-kh]').forEach(el => {
            const val = isKh ? el.getAttribute('data-kh') : (el.getAttribute('data-en') || el.getAttribute('data-original-text'));
            if (!val) return;
            if (!el.getAttribute('data-original-text')) el.setAttribute('data-original-text', el.innerHTML);
            if (val.includes('<') && val.includes('>')) { el.innerHTML = val; } else { el.textContent = val; }
        });

        // 2. Placeholders
        document.querySelectorAll('[data-placeholder-kh]').forEach(el => {
            el.placeholder = isKh
                ? el.getAttribute('data-placeholder-kh')
                : (el.getAttribute('data-placeholder-en') || el.placeholder);
        });

        document.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(input => {
            const ph = input.placeholder.trim();
            if (!input.getAttribute('data-original-ph') && ph) {
                input.setAttribute('data-original-ph', ph);
            }
            const origPh = input.getAttribute('data-original-ph');
            if (origPh) {
                input.placeholder = isKh ? translateDynamicText(origPh, true) : origPh;
            }
        });

        // 3. Select options translation
        document.querySelectorAll('select option').forEach(opt => {
            const text = opt.textContent.trim();
            if (!opt.getAttribute('data-original-opt') && text) {
                opt.setAttribute('data-original-opt', text);
            }
            const origOpt = opt.getAttribute('data-original-opt');
            if (origOpt) {
                opt.textContent = isKh ? translateDynamicText(origOpt, true) : origOpt;
            }
        });

        // 4. Universal text node walker across all content elements
        const textElements = document.querySelectorAll(
            'h1, h2, h3, h4, h5, h6, p, span, a, button, label, td, th, li, b, strong, small, .clothe-nav-link, .sidebar-menu a, .admin-card-title'
        );

        textElements.forEach(el => {
            // Avoid touching elements that have child tags if we can target leaf elements
            for (let node of el.childNodes) {
                if (node.nodeType === Node.TEXT_NODE) {
                    const text = node.textContent.trim();
                    if (!text || text.length < 2) continue;
                    // Don't translate pure numbers or symbols
                    if (/^[\d\s$.,:;#/\\~%()\-+*!]+$/.test(text)) continue;

                    if (!node._origText) {
                        node._origText = text;
                    }
                    const orig = node._origText;
                    if (isKh) {
                        const translated = translateDynamicText(orig, true);
                        if (translated !== orig) {
                            node.textContent = node.textContent.replace(orig, translated);
                        }
                    } else if (orig) {
                        node.textContent = node.textContent.replace(node.textContent.trim(), orig);
                    }
                }
            }
        });

        updateLangControlsUI(lang);
    }

    // =========================================================
    //  INIT
    // =========================================================
    applyTheme(getStoredTheme());

    document.addEventListener('DOMContentLoaded', function () {
        applyTheme(getStoredTheme());
        translatePage(getStoredLang());

        // Observe DOM changes (modals, ajax loads) to keep Khmer translations active
        const observer = new MutationObserver(function (mutations) {
            const currentLang = getStoredLang();
            if (currentLang === 'kh') {
                // Throttle slightly
                clearTimeout(window._i18nMutationTimer);
                window._i18nMutationTimer = setTimeout(() => {
                    translatePage('kh');
                }, 100);
            }
        });

        if (document.body) {
            observer.observe(document.body, { childList: true, subtree: true });
        }

        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
                if (!localStorage.getItem('site_theme')) {
                    applyTheme(e.matches ? 'dark' : 'light');
                }
            });
        }
    });

    // =========================================================
    //  GLOBAL API
    // =========================================================
    window.toggleGlobalTheme = function () {
        const next = getStoredTheme() === 'dark' ? 'light' : 'dark';
        localStorage.setItem('site_theme', next);
        applyTheme(next);
    };

    window.setGlobalLanguage = function (lang) {
        localStorage.setItem('site_language', lang);
        translatePage(lang);
        window.dispatchEvent(new CustomEvent('site_language_changed', { detail: { lang } }));
    };

    window.toggleTheme = window.toggleGlobalTheme;
    window.setLanguage = window.setGlobalLanguage;

})();
