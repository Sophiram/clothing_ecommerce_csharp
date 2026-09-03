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