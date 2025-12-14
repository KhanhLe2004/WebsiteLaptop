(function ($) {
    "use strict";

    // Spinner
    var spinner = function () {
        setTimeout(function () {
            if ($('#spinner').length > 0) {
                $('#spinner').removeClass('show');
            }
        }, 1);
    };
    spinner(0);


    // Fixed Navbar
    $(window).scroll(function () {
        if ($(window).width() < 992) {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow');
            } else {
                $('.fixed-top').removeClass('shadow');
            }
        } else {
            if ($(this).scrollTop() > 55) {
                $('.fixed-top').addClass('shadow').css('top', -55);
            } else {
                $('.fixed-top').removeClass('shadow').css('top', 0);
            }
        } 
    });
    
    
   // Back to top button
   $(window).scroll(function () {
    if ($(this).scrollTop() > 300) {
        $('.back-to-top').fadeIn('slow');
    } else {
        $('.back-to-top').fadeOut('slow');
    }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });


    // Testimonial carousel
    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 2000,
        center: false,
        dots: true,
        loop: true,
        margin: 25,
        nav : true,
        navText : [
            '<i class="bi bi-arrow-left"></i>',
            '<i class="bi bi-arrow-right"></i>'
        ],
        responsiveClass: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:1
            },
            992:{
                items:2
            },
            1200:{
                items:2
            }
        }
    });


    // vegetable carousel
    $(".vegetable-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1500,
        center: false,
        dots: true,
        loop: true,
        margin: 25,
        nav : true,
        navText : [
            '<i class="bi bi-arrow-left"></i>',
            '<i class="bi bi-arrow-right"></i>'
        ],
        responsiveClass: true,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:1
            },
            768:{
                items:2
            },
            992:{
                items:3
            },
            1200:{
                items:4
            }
        }
    });


    // Product details thumbnail carousel (thumbnails under single product)
    $(".product__details__pic__slider").owlCarousel({
        autoplay: false,
        smartSpeed: 800,
        center: false,
        dots: false,
        loop: false,
        margin: 20,
        nav : true,
        navText : [
            '<i class="bi bi-arrow-left"></i>',
            '<i class="bi bi-arrow-right"></i>'
        ],
        responsiveClass: true,
        responsive: {
            0:{ items:3 },
            576:{ items:4 },
            768:{ items:4 },
            992:{ items:4 }
        }
    });


    // Modal Video
    $(document).ready(function () {
        var $videoSrc;
        $('.btn-play').click(function () {
            $videoSrc = $(this).data("src");
        });
        console.log($videoSrc);

        $('#videoModal').on('shown.bs.modal', function (e) {
            $("#video").attr('src', $videoSrc + "?autoplay=1&amp;modestbranding=1&amp;showinfo=0");
        })

        $('#videoModal').on('hide.bs.modal', function (e) {
            $("#video").attr('src', $videoSrc);
        })
    });



    // Product Quantity
    $('.quantity button').on('click', function () {
        var button = $(this);
        var oldValue = button.parent().parent().find('input').val();
        if (button.hasClass('btn-plus')) {
            var newVal = parseFloat(oldValue) + 1;
        } else {
            if (oldValue > 0) {
                var newVal = parseFloat(oldValue) - 1;
            } else {
                newVal = 0;
            }
        }
        button.parent().parent().find('input').val(newVal);
    });

})(jQuery);


(function(){
  const container = document.querySelector('.hero__categories');
  const toggle = document.getElementById('categoriesToggle');
  const list = document.getElementById('categoriesList');

  if (!container || !toggle || !list) return;

  // Bắt đầu ở trạng thái mở mặc định
  container.classList.add('open');
  toggle.setAttribute('aria-expanded', 'true');

  // Click để toggle (mở <-> đóng). Chỉ click vào toggle mới thay đổi.
  toggle.addEventListener('click', function (e) {
    e.preventDefault();
    const isOpen = container.classList.toggle('open');
    toggle.setAttribute('aria-expanded', String(isOpen));
  });

  // Keyboard accessible: Enter / Space sẽ kích hoạt toggle
  toggle.addEventListener('keydown', function(e){
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggle.click();
    }
  });
})();


(function(){
  // chọn tất cả các parent items có submenu
  const parents = document.querySelectorAll('.category-dropdown .js-has-sub');

  if (!parents.length) return;

  parents.forEach(anchor => {
    const parent = anchor.closest('.category-dropdown');

    // ensure parent exists
    if (!parent) return;

    // prevent default anchor navigation
    anchor.addEventListener('click', function(e){
      e.preventDefault();

      // toggle 'open' on the parent
      const isOpen = parent.classList.toggle('open');

      // update aria-expanded on anchor
      anchor.setAttribute('aria-expanded', String(isOpen));

      // optionally close other open siblings (if you want only one open at a time)
      // close siblings:
      // Array.from(document.querySelectorAll('.category-dropdown.open'))
      //   .filter(p => p !== parent)
      //   .forEach(p => {
      //     p.classList.remove('open');
      //     const a = p.querySelector('.js-has-sub');
      //     if (a) a.setAttribute('aria-expanded','false');
      //   });
    });

    // Optional: close with Escape when focused inside submenu
    parent.addEventListener('keydown', function(e){
      if (e.key === 'Escape' && parent.classList.contains('open')) {
        parent.classList.remove('open');
        anchor.setAttribute('aria-expanded','false');
        anchor.focus();
      }
    });

    // Prevent hover from re-opening if you use hover rules elsewhere:
    // If you have CSS .category-dropdown:hover > .dropdown-menu { display:block; }
    // either remove it, or block it here by listening pointerenter and checking a flag.
    // Below: if the parent has data attribute suppress-hover="true", ignore pointerenter.
    parent.addEventListener('pointerenter', function(e){
      // If parent was opened/closed via click we don't want pointerenter to force open.
      // So do nothing here (we control open via .open class only).
    });

  });

})();

// Đóng flyout khi click vào 1 item trong submenu
(function(){
  // lắng nghe sự kiện click từ document (delegation)
  document.addEventListener('click', function(e){
    // tìm phần tử <a> trong submenu đã click (nếu có)
    const a = e.target.closest('.category-dropdown .dropdown-menu a.dropdown-item');
    if (!a) return;

    // tìm parent .category-dropdown
    const parent = a.closest('.category-dropdown');
    if (!parent) return;

    // gỡ class .open để ẩn flyout
    parent.classList.remove('open');

    // cập nhật aria-expanded cho anchor toggle trong parent (nếu có)
    const anchorToggle = parent.querySelector('.js-has-sub') || parent.querySelector('.dropdown-toggle');
    if (anchorToggle) anchorToggle.setAttribute('aria-expanded', 'false');

    // Lưu ý: không preventDefault() để link vẫn hoạt động (nếu là link chuyển trang).
    // Nếu bạn muốn delay đóng để cho animation chạy trước khi chuyển trang, có thể:
    // e.preventDefault();
    // setTimeout(() => { window.location = a.href; }, 120);
  }, false);
})();

/* Product detail: main image prev/next + thumbnail click behavior */
(function($){
    $(function(){
        var $main = $('.main-image');
        var $thumbs = $('.product__details__pic__slider img');

        if (!$main.length || !$thumbs.length) return;

        // Build gallery array from data-imgbigurl or src
        var gallery = $thumbs.map(function(){
            return $(this).data('imgbigurl') || $(this).attr('src');
        }).get();

        // current index: try to match main src to a gallery item, otherwise default to 0
        var mainSrc = $main.attr('src');
        var currentIndex = gallery.indexOf(mainSrc);
        if (currentIndex === -1) {
            currentIndex = 0;
            // initialize main image to first gallery image for consistent behavior
            $main.attr('src', gallery[0]);
        }

        function updateMain(i){
            if (!gallery.length) return;
            currentIndex = (i % gallery.length + gallery.length) % gallery.length;
            $main.attr('src', gallery[currentIndex]);
            // highlight thumbnail
            $thumbs.closest('.item').removeClass('thumb-active');
            $thumbs.eq(currentIndex).closest('.item').addClass('thumb-active');
        }

        // thumbnail click
        $thumbs.on('click', function(e){
            var idx = $thumbs.index(this);
            updateMain(idx);
        });

        // prev / next buttons
        $('.main-nav.prev').on('click', function(e){
            e.preventDefault();
            updateMain(currentIndex - 1);
        });
        $('.main-nav.next').on('click', function(e){
            e.preventDefault();
            updateMain(currentIndex + 1);
        });

        // initial highlight
        $thumbs.eq(currentIndex).closest('.item').addClass('thumb-active');
    });
})(jQuery);

$(document).ready(function(){
  $(".product__details__pic__slider .item img").on("click", function(){
    var bigImage = $(this).attr("data-imgbigurl"); // lấy link ảnh lớn
    $(".main-image").attr("src", bigImage); // đổi src của ảnh chính
  });

  // Khởi tạo Owl Carousel nếu chưa có
  $(".product__details__pic__slider").owlCarousel({
    margin: 10,
    nav: true,
    dots: false,
    items: 3
  });
});
