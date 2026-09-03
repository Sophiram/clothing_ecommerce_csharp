document.addEventListener("DOMContentLoaded", function () {

    /* =====================================================
       ELEMENTS
    ====================================================== */

    const sidebar =
        document.getElementById("adminSidebar");

    const toggleButton =
        document.getElementById("sidebarToggle");

    const overlay =
        document.getElementById("sidebarOverlay");

    const adminMain =
        document.getElementById("adminMain");


    /* =====================================================
       CONFIG
    ====================================================== */

    const MOBILE_BREAKPOINT = 991;

    const STORAGE_KEY =
        "clotheAdminSidebarCollapsed";


    /* =====================================================
       SAFETY CHECK
    ====================================================== */

    if (!sidebar || !toggleButton) {

        console.warn(
            "CLOTHÉ Admin: Sidebar elements not found."
        );

        return;
    }


    /* =====================================================
       HELPERS
    ====================================================== */

    function isMobile() {

        return window.innerWidth <=
            MOBILE_BREAKPOINT;
    }


    function setAriaExpanded(value) {

        toggleButton.setAttribute(
            "aria-expanded",
            value ? "true" : "false"
        );
    }


    function setSidebarAriaHidden(value) {

        sidebar.setAttribute(
            "aria-hidden",
            value ? "true" : "false"
        );
    }


    /* =====================================================
       MOBILE OPEN
    ====================================================== */

    function openMobileSidebar() {

        if (!isMobile()) {
            return;
        }

        sidebar.classList.add("show");

        if (overlay) {
            overlay.classList.add("show");
        }

        document.body.classList.add(
            "sidebar-open"
        );

        setAriaExpanded(true);

        setSidebarAriaHidden(false);
    }


    /* =====================================================
       MOBILE CLOSE
    ====================================================== */

    function closeMobileSidebar() {

        sidebar.classList.remove("show");

        if (overlay) {
            overlay.classList.remove("show");
        }

        document.body.classList.remove(
            "sidebar-open"
        );

        setAriaExpanded(false);

        setSidebarAriaHidden(true);
    }


    /* =====================================================
       MOBILE TOGGLE
    ====================================================== */

    function toggleMobileSidebar() {

        if (
            sidebar.classList.contains("show")
        ) {

            closeMobileSidebar();

        } else {

            openMobileSidebar();
        }
    }


    /* =====================================================
       DESKTOP COLLAPSE
    ====================================================== */

    function collapseDesktopSidebar() {

        if (isMobile()) {
            return;
        }

        sidebar.classList.add(
            "collapsed"
        );

        if (adminMain) {

            adminMain.classList.add(
                "sidebar-collapsed"
            );
        }

        localStorage.setItem(
            STORAGE_KEY,
            "true"
        );

        setAriaExpanded(false);

        setSidebarAriaHidden(false);
    }


    /* =====================================================
       DESKTOP EXPAND
    ====================================================== */

    function expandDesktopSidebar() {

        if (isMobile()) {
            return;
        }

        sidebar.classList.remove(
            "collapsed"
        );

        if (adminMain) {

            adminMain.classList.remove(
                "sidebar-collapsed"
            );
        }

        localStorage.setItem(
            STORAGE_KEY,
            "false"
        );

        setAriaExpanded(true);

        setSidebarAriaHidden(false);
    }


    /* =====================================================
       DESKTOP TOGGLE
    ====================================================== */

    function toggleDesktopSidebar() {

        if (
            sidebar.classList.contains(
                "collapsed"
            )
        ) {

            expandDesktopSidebar();

        } else {

            collapseDesktopSidebar();
        }
    }


    /* =====================================================
       INITIALIZE
    ====================================================== */

    function initializeSidebar() {

        const mobile =
            isMobile();

        const savedState =
            localStorage.getItem(
                STORAGE_KEY
            );


        /* MOBILE */

        if (mobile) {

            sidebar.classList.remove(
                "collapsed"
            );

            if (adminMain) {

                adminMain.classList.remove(
                    "sidebar-collapsed"
                );
            }

            closeMobileSidebar();

            return;
        }


        /* DESKTOP */

        closeMobileSidebar();

        setSidebarAriaHidden(false);


        if (savedState === "true") {

            sidebar.classList.add(
                "collapsed"
            );

            if (adminMain) {

                adminMain.classList.add(
                    "sidebar-collapsed"
                );
            }

            setAriaExpanded(false);

        } else {

            sidebar.classList.remove(
                "collapsed"
            );

            if (adminMain) {

                adminMain.classList.remove(
                    "sidebar-collapsed"
                );
            }

            setAriaExpanded(true);
        }
    }


    /* =====================================================
       ARIA
    ====================================================== */

    toggleButton.setAttribute(
        "aria-controls",
        "adminSidebar"
    );


    /* =====================================================
       TOGGLE BUTTON
    ====================================================== */

    toggleButton.addEventListener(
        "click",
        function (event) {

            event.preventDefault();

            if (isMobile()) {

                toggleMobileSidebar();

            } else {

                toggleDesktopSidebar();
            }
        }
    );


    /* =====================================================
       OVERLAY
    ====================================================== */

    if (overlay) {

        overlay.addEventListener(
            "click",
            function () {

                closeMobileSidebar();

            }
        );
    }


    /* =====================================================
       SIDEBAR LINKS
    ====================================================== */

    const sidebarLinks =
        document.querySelectorAll(
            ".sidebar-menu a"
        );


    sidebarLinks.forEach(
        function (link) {

            link.addEventListener(
                "click",
                function () {

                    if (isMobile()) {

                        closeMobileSidebar();
                    }

                }
            );

        }
    );


    /* =====================================================
       TOOLTIP
    ====================================================== */

    sidebarLinks.forEach(
        function (link) {

            const textElement =
                link.querySelector(
                    "span"
                );

            if (!textElement) {
                return;
            }

            const text =
                textElement.textContent.trim();

            if (text) {

                link.setAttribute(
                    "data-tooltip",
                    text
                );
            }

        }
    );


    /* =====================================================
       ESCAPE KEY
    ====================================================== */

    document.addEventListener(
        "keydown",
        function (event) {

            if (
                event.key !== "Escape"
            ) {
                return;
            }


            if (isMobile()) {

                closeMobileSidebar();

            }

        }
    );


    /* =====================================================
       RESIZE
    ====================================================== */

    let resizeTimer = null;


    window.addEventListener(
        "resize",
        function () {

            clearTimeout(
                resizeTimer
            );


            resizeTimer =
                setTimeout(
                    function () {

                        initializeSidebar();

                    },
                    150
                );

        }
    );


    /* =====================================================
       CLICK OUTSIDE PROFILE DROPDOWN
       Bootstrap handles the dropdown itself.
    ====================================================== */


    /* =====================================================
       ACTIVE LINK
    ====================================================== */

    function updateActiveMenu() {

        const currentPath =
            window.location.pathname
                .toLowerCase()
                .replace(/\/+$/, "");


        sidebarLinks.forEach(
            function (link) {

                const href =
                    link.getAttribute(
                        "href"
                    );


                if (
                    !href ||
                    href === "#" ||
                    href.startsWith(
                        "javascript:"
                    )
                ) {
                    return;
                }


                try {

                    const url =
                        new URL(
                            href,
                            window.location.origin
                        );


                    let linkPath =
                        url.pathname
                            .toLowerCase()
                            .replace(
                                /\/+$/,
                                ""
                            );


                    /*
                     * Don't automatically mark
                     * the root "/" as active.
                     */

                    if (
                        linkPath === ""
                    ) {
                        return;
                    }


                    /*
                     * Compare exact path first.
                     */

                    if (
                        currentPath ===
                        linkPath
                    ) {

                        link.classList.add(
                            "active"
                        );

                        return;
                    }


                    /*
                     * Compare child routes.
                     */

                    if (
                        currentPath.startsWith(
                            linkPath + "/"
                        )
                    ) {

                        link.classList.add(
                            "active"
                        );

                    } else {

                        /*
                         * Keep Razor's server-side
                         * active class when appropriate.
                         */

                        if (
                            !link.classList.contains(
                                "active"
                            )
                        ) {

                            link.classList.remove(
                                "active"
                            );
                        }
                    }

                } catch (error) {

                    console.warn(
                        "CLOTHÉ Admin: Could not process sidebar link.",
                        href,
                        error
                    );

                }

            }
        );
    }


    /* =====================================================
       INITIAL LOAD
    ====================================================== */

    initializeSidebar();

    updateActiveMenu();


    /* =====================================================
       DEBUG
    ====================================================== */

    console.log(
        "CLOTHÉ Admin UI initialized."
    );

});