// Page Load Animations - Hiệu ứng mới mượt mà

(function() {
    'use strict';

    // Class animation
    const animationClass = 'animate-left';

    // Các selector cho các phần tử cần animation - chỉ các phần tử chính
    const selectors = [
        'main > .container',
        'main > section',
        '.row:first-of-type',
        '.card'
    ];

    // Hàm thêm animation cho phần tử
    function animateElement(element, delay = 0) {
        // Bỏ qua nếu đã có animation hoặc là phần tử ẩn
        if (element.classList.contains(animationClass) || 
            element.offsetParent === null ||
            element.style.display === 'none' ||
            element.closest('.no-animate') ||
            element.id === 'megaMenu' ||
            element.closest('#megaMenu')) {
            return;
        }

        // Thêm animation với delay
        requestAnimationFrame(() => {
            setTimeout(() => {
                element.classList.add(animationClass);
            }, delay);
        });
    }

    // Hàm xử lý các phần tử khi trang load
    function initAnimations() {
        // Chờ DOM sẵn sàng
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                setTimeout(processElements, 100);
            });
        } else {
            setTimeout(processElements, 100);
        }
    }

    // Xử lý các phần tử
    function processElements() {
        const elements = [];
        
        selectors.forEach(selector => {
            try {
                document.querySelectorAll(selector).forEach(el => {
                    if (el.offsetHeight > 0 && el.offsetWidth > 0 && 
                        !el.closest('.no-animate') &&
                        !el.classList.contains('no-animate') &&
                        !el.classList.contains(animationClass)) {
                        
                        let isChild = false;
                        for (let existingEl of elements) {
                            if (existingEl.contains(el)) {
                                isChild = true;
                                break;
                            }
                        }
                        
                        if (!isChild) {
                            elements.push(el);
                        }
                    }
                });
            } catch (e) {
                // Bỏ qua selector không hợp lệ
            }
        });

        // Sắp xếp theo vị trí
        elements.sort((a, b) => {
            const rectA = a.getBoundingClientRect();
            const rectB = b.getBoundingClientRect();
            
            if (Math.abs(rectA.top - rectB.top) > 50) {
                return rectA.top - rectB.top;
            }
            return rectA.left - rectB.left;
        });

        // Giới hạn số lượng phần tử
        const maxElements = 15;
        const elementsArray = elements.slice(0, maxElements);

        // Áp dụng animation với delay tăng dần
        elementsArray.forEach((element, index) => {
            const delay = index * 60; // Delay 60ms giữa các phần tử
            animateElement(element, delay);
        });
    }

    // ===== Scroll Reveal - Lướt đến đâu hiện ra đến đấy =====
    
    function initScrollReveal() {
        // Kiểm tra xem có hỗ trợ Intersection Observer không
        if (!('IntersectionObserver' in window)) {
            // Fallback: hiện tất cả element ngay lập tức
            document.querySelectorAll('.scroll-reveal, .scroll-reveal-left, .scroll-reveal-right, .scroll-reveal-scale').forEach(el => {
                el.classList.add('revealed');
            });
            return;
        }

        // Cấu hình Intersection Observer
        const observerOptions = {
            root: null, // viewport
            rootMargin: '0px 0px -50px 0px', // Trigger khi element còn cách viewport 50px
            threshold: 0.1 // Trigger khi 10% element hiển thị
        };

        // Callback khi element vào viewport
        const observerCallback = (entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    // Thêm class 'revealed' để trigger animation
                    entry.target.classList.add('revealed');
                    // Ngừng observe element này để tối ưu performance
                    observer.unobserve(entry.target);
                }
            });
        };

        // Tạo observer
        const observer = new IntersectionObserver(observerCallback, observerOptions);

        // Tự động thêm class scroll-reveal cho các element phổ biến
        const autoRevealSelectors = [
            '.card',
            '.row > .col',
            'section',
            'article',
            '.book-card',
            '.product-item',
            '.category-item',
            'h2, h3, h4',
            '.card-body',
            '.list-group-item',
            '.table-responsive',
            '.table-modern',
            'form.card'
        ];

        // Thêm class scroll-reveal cho các element chưa có
        autoRevealSelectors.forEach(selector => {
            try {
                document.querySelectorAll(selector).forEach(el => {
                    // Bỏ qua nếu đã có class scroll-reveal hoặc nằm trong no-animate
                    if (!el.classList.contains('scroll-reveal') &&
                        !el.classList.contains('scroll-reveal-left') &&
                        !el.classList.contains('scroll-reveal-right') &&
                        !el.classList.contains('scroll-reveal-scale') &&
                        !el.closest('.no-animate') &&
                        !el.classList.contains('no-animate') &&
                        el.offsetHeight > 0 &&
                        el.offsetWidth > 0 &&
                        el.id !== 'megaMenu' &&
                        !el.closest('#megaMenu') &&
                        !el.closest('header') &&
                        !el.closest('nav') &&
                        !el.closest('.admin-sidebar') &&
                        !el.closest('.admin-header')) {
                        
                        // Thêm class scroll-reveal mặc định
                        el.classList.add('scroll-reveal');
                    }
                });
            } catch (e) {
                // Bỏ qua selector không hợp lệ
            }
        });

        // Observe tất cả element có class scroll-reveal
        document.querySelectorAll('.scroll-reveal, .scroll-reveal-left, .scroll-reveal-right, .scroll-reveal-scale').forEach(el => {
            observer.observe(el);
        });

        // Observe các element mới được thêm vào DOM (cho dynamic content)
        const mutationObserver = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === 1) { // Element node
                        // Kiểm tra element mới
                        if (node.classList && (
                            node.classList.contains('scroll-reveal') ||
                            node.classList.contains('scroll-reveal-left') ||
                            node.classList.contains('scroll-reveal-right') ||
                            node.classList.contains('scroll-reveal-scale')
                        )) {
                            observer.observe(node);
                        }
                        // Kiểm tra các element con
                        node.querySelectorAll && node.querySelectorAll('.scroll-reveal, .scroll-reveal-left, .scroll-reveal-right, .scroll-reveal-scale').forEach(el => {
                            observer.observe(el);
                        });
                    }
                });
            });
        });

        // Bắt đầu observe DOM changes
        mutationObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Khởi tạo
    initAnimations();
    
    // Khởi tạo scroll reveal
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initScrollReveal);
    } else {
        initScrollReveal();
    }

    // Export function để có thể gọi lại khi cần
    window.reinitPageAnimations = function() {
        processElements();
    };
    
    window.reinitScrollReveal = function() {
        initScrollReveal();
    };
})();
