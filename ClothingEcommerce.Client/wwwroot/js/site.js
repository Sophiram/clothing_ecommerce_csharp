document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll("img").forEach(function (img) {

        img.addEventListener("error", function () {

            if (!img.dataset.failed) {

                img.dataset.failed = "1";

                img.src =
                    "https://via.placeholder.com/600x700?text=Image";

            }

        });

    });

});


document.addEventListener('click', async function (e) {
    const mobileBtn = e.target.closest('.add-cart-mobile-btn');
    if (mobileBtn) {
        const form = mobileBtn.closest('.product-card-modern')?.querySelector('.quick-add-form');
        if (form) form.requestSubmit();
    }

    const wishBtn = e.target.closest('.wishlist-btn:not(.js-wishlist-btn)');
    if (wishBtn) {
        wishBtn.classList.toggle('active');
    }
});