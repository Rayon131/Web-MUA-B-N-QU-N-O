let productsData = [], customersData = [], promotionsData = [];
let orders = [];
let currentOrderIndex = -1;
let html5QrcodeScanner;
const MAX_PENDING_ORDERS = 5;
let promotionRule = 'BestValue';

// =================================================================
// KHỞI TẠO (INITIALIZATION)
// =================================================================
$(document).ready(function () {
    if (typeof apiUrls === 'undefined') {
        console.error("Lỗi cấu hình: Biến 'apiUrls' chưa được định nghĩa trong View.");
        return;
    }

    $('#start-first-order-btn').on('click', startFirstOrder);
    $('#create-new-order-btn').on('click', createNewPendingOrder);
    $('#cancel-order-btn').on('click', cancelCurrentOrder);
    $('#addProductToCartBtn').on('click', addProductToCartFromModal);
    $('#save-order-btn').on('click', () => processOrder(1));
    $('#checkout-btn').on('click', () => processOrder(2));
    $('#payment-method-select').on('change', toggleCashPaymentSection);
    $('#cash-received-input').on('input', renderCart);
    $('#scan-qr-btn').on('click', startQrScanner);
    $('#qrScannerModal').on('hidden.bs.modal', stopQrScanner);
    $('#qr-input-file').on('change', handleQrFileUpload);
    $('#remove-voucher-btn').on('click', removeVoucher); 
    // === SỰ KIỆN CHO VOUCHER ===
    $('#apply-voucher-btn').on('click', applyVoucher);
    initializeVoucherSelect();
    const urlParams = new URLSearchParams(window.location.search);
    const paymentSuccess = urlParams.get('payment_success');
    const orderId = urlParams.get('orderId');

    if (paymentSuccess && orderId) {
        if (confirm('Thanh toán bằng chuyển khoản thành công! Bạn có muốn in hóa đơn không?')) {
            const printUrl = `${apiUrls.printReceipt}?orderId=${orderId}`;
            window.open(printUrl, '_blank');
        }
        // Xóa các tham số khỏi URL để không bị hỏi lại khi F5
        window.history.replaceState({}, document.title, window.location.pathname);
    }
    $('#cash-received-input').on('keydown', function (e) {
        if (e.key === '-' || e.key === 'e') {
            e.preventDefault();
        }
    });
    fetchInitialData();
    toggleCashPaymentSection();
});

// =================================================================
// TẢI DỮ LIỆU BAN ĐẦU (INITIAL DATA FETCHING)
// =================================================================
async function fetchInitialData() {
    try {
        const response = await fetch(apiUrls.getInitialData);
        if (!response.ok) throw new Error(`Lỗi server ${response.status}`);
        const data = await response.json();
        productsData = data.products;
        customersData = data.customers;
        promotionsData = data.promotions;
        orders = (data.pendingOrders || []).map((o, idx) => ({
            id: o.maDonHang,
            displayId: idx + 1,
            status: (o.items && o.items.length > 0) ? o.trangThai : 0, 
            items: o.items || [],
            customerId: o.maKhachHang,
            paymentMethod: o.phuongThucThanhToan,
            // Nếu API trả về voucher (o.voucher), dùng nó. Nếu không, tạo object rỗng.
            voucher: o.voucher || { id: null, code: '', loaiGiamGia: null, giaTriGiamGia: 0 }
        }));
        renderProducts(productsData);
        populateDropdowns();
        if (orders.length > 0) {
            $('#initial-order-prompt').hide();
            $('#order-section-wrapper').show();
            switchToOrder(0);
        } else {
            $('#order-section-wrapper').hide();
            $('#initial-order-prompt').show();
        }
    } catch (error) {
        console.error('Lỗi khi tải dữ liệu ban đầu:', error);
    }
}


// =================================================================
// QUẢN LÝ HÓA ĐƠN (ORDER MANAGEMENT)
// =================================================================
async function startFirstOrder() {
    await createAndSwitchToNewOrder();

    // Thêm dòng này để cưỡng chế ẩn màn hình chào
    $('#initial-order-prompt').hide();
    $('#order-section-wrapper').show();
}

async function createNewPendingOrder() {
    // Bỏ qua kiểm tra hóa đơn trống, cho phép tạo luôn
    await createAndSwitchToNewOrder();
}

async function createAndSwitchToNewOrder() {
    if (orders.length >= MAX_PENDING_ORDERS) {
        alert(`Bạn chỉ có thể mở tối đa ${MAX_PENDING_ORDERS} hóa đơn chờ.`);
        return;
    }
    try {
        const response = await fetch(apiUrls.createPendingOrder, { method: 'POST' });
        const result = await response.json();
        if (response.ok && result.success) {
            const newOrder = {
                id: result.orderId,
                // <--- SỬA DÒNG DƯỚI ĐÂY: Lấy độ dài mảng + 1
                displayId: orders.length + 1,
                items: [],
                customerId: null,
                status: 0,
                voucher: { id: null, code: '', loaiGiamGia: null, giaTriGiamGia: 0 }
            };
            orders.push(newOrder);
            switchToOrder(orders.length - 1);
        } else {
            alert("Lỗi khi tạo hóa đơn chờ.");
        }
    } catch (error) {
        console.error("Lỗi API tạo hóa đơn:", error);
    }
}


async function processOrder(status) {
    const currentOrder = orders[currentOrderIndex];
    if (!currentOrder) return alert('Không có hóa đơn nào được chọn.');

    // Logic trạng thái thực tế
    let realStatus = status;
    if (status === 1 && currentOrder.items.length === 0) {
        realStatus = 0;
    }

    const paymentMethod = $('#payment-method-select').val();

    // 1. Chặn thanh toán ngay khi giỏ trống
    if (status === 2 && currentOrder.items.length === 0) {
        return alert('Không thể thanh toán hóa đơn trống.');
    }

    // === 2. LOGIC KIỂM TRA TIỀN KHÁCH ĐƯA (SỬA Ở ĐÂY) ===
    const cashInputStr = $('#cash-received-input').val().trim();
    const cashReceived = parseCurrency(cashInputStr);

    // Tính tổng tiền cần thanh toán
    const subTotal = currentOrder.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
    const voucher = currentOrder.voucher;
    let discount = 0;
    if (voucher && voucher.id) {
        discount = (voucher.loaiGiamGia === 'PhanTram')
            ? subTotal * (voucher.giaTriGiamGia / 100)
            : voucher.giaTriGiamGia;
    }
    const finalTotal = Math.max(0, subTotal - discount);

    // Chỉ kiểm tra khi: Thanh toán bằng Tiền mặt VÀ Có nhập số tiền
    if (status === 2 && paymentMethod === 'Tiền mặt' && cashInputStr !== "") {

        // Kiểm tra số âm
        if (cashReceived < 0) {
            return alert("Số tiền khách đưa không được là số âm!");
        }

        // Kiểm tra thiếu tiền (Cho phép sai số nhỏ < 100đ do làm tròn)
        if (cashReceived < finalTotal - 100) {
            const missing = (finalTotal - cashReceived).toLocaleString('vi-VN');
            return alert(`Khách đưa thiếu tiền! Còn thiếu: ${missing}đ`);
        }
    }
    // ========================================================

    const orderData = {
        orderId: currentOrder.id,
        customerId: $('#customer-select').val() ? parseInt($('#customer-select').val()) : null,
        paymentMethod: paymentMethod,
        tienMatDaNhan: (paymentMethod === 'Tiền mặt' && cashInputStr === "") ? finalTotal : cashReceived,
        status: realStatus,
        maGiamGiaId: currentOrder.voucher ? currentOrder.voucher.id : null,
        orderDetails: currentOrder.items.map(item => ({
            productDetailId: item.productDetailId,
            quantity: item.quantity,
            unitPrice: item.unitPrice
        }))
    };

    try {
        const response = await fetch(apiUrls.updateOrder, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(orderData)
        });
        const result = await response.json();

        if (response.ok && result.success) {
            // === CẬP NHẬT GIAO DIỆN ===
            currentOrder.status = realStatus;
            currentOrder.paymentMethod = paymentMethod; // Lưu lại phương thức thanh toán
            renderOrderTabs();

            // Logic chuyển trang hoặc in hóa đơn
            if (result.redirectTo) {
                // Nếu là chuyển khoản và nhấn "Thanh toán ngay" (status=2) -> Chuyển sang VNPay
                window.location.href = result.redirectTo;
            }
            else if (status === 2 && result.orderId) {
                // Nếu là tiền mặt và nhấn "Thanh toán ngay" -> In hóa đơn
                if (confirm('Thanh toán thành công! Bạn có muốn in hóa đơn không?')) {
                    const printUrl = `${apiUrls.printReceipt}?orderId=${result.orderId}`;
                    window.open(printUrl, '_blank');
                }
                removeOrderFromUI(currentOrder.id);
            }
            else {
                // Nếu chỉ nhấn LƯU (status=1) -> Thông báo thành công
                alert(result.message || "Đã lưu đơn hàng thành công!");
                // Không cần fetchInitialData để tránh mất trạng thái hiện tại
            }
        } else {
            alert(`Lỗi: ${result.message || 'Không thể cập nhật đơn hàng.'}`);
        }
    } catch (error) {
        console.error('Lỗi khi cập nhật đơn hàng:', error);
        alert('Lỗi kết nối máy chủ.');
    }
}

async function cancelCurrentOrder() {
    const currentOrder = orders[currentOrderIndex];
    if (!currentOrder) return alert('Không có hóa đơn nào được chọn.');

    // Đổi thông báo từ "hủy" thành "xóa" để nhân viên biết đây là thao tác xóa vĩnh viễn
    if (!confirm(`Bạn có chắc muốn hủy Hóa đơn ${currentOrder.displayId}? Thao tác này sẽ không thể hoàn tác.`)) return;

    try {
        const response = await fetch(apiUrls.cancelOrder, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ orderId: currentOrder.id })
        });
        const result = await response.json();

        if (response.ok && result.success) {
            alert(result.message);
            // Hàm này sẽ xóa tab và cập nhật lại giao diện như bạn đã viết
            removeOrderFromUI(currentOrder.id);
        } else {
            alert(`Lỗi: ${result.message}`);
        }
    } catch (error) {
        console.error('Lỗi API xóa đơn hàng:', error);
        alert('Lỗi kết nối máy chủ.');
    }
}

// =================================================================
// HIỂN THỊ GIAO DIỆN (UI RENDERING & INTERACTIONS)
// =================================================================
function renderProducts(products) {
    const productsList = $('#products-list').empty();

    if (!products || products.length === 0) {
        return productsList.html('<tr><td colspan="6" class="text-center text-muted py-4">Không tìm thấy sản phẩm nào.</td></tr>');
    }

    products.forEach((p, index) => {
        const promotionBadge = p.hasPromotion ? '<span class="badge-sale">SALE</span>' : '';

        // === LOGIC HIỂN THỊ SỐ LƯỢNG ===
        // Lưu ý: Trong JS, thuộc tính C# 'TotalStock' thường tự chuyển thành 'totalStock' (chữ thường đầu)
        const stock = p.totalStock || 0;
        let stockDisplay = '';

        if (stock <= 0) {
            stockDisplay = '<span class="badge bg-danger">Hết hàng</span>';
        } else if (stock < 10) {
            stockDisplay = `<span class="fw-bold text-warning">${stock}</span>`; // Sắp hết thì màu vàng
        } else {
            stockDisplay = `<span class="fw-bold text-success">${stock}</span>`; // Còn nhiều thì màu xanh
        }
        // ================================

        const rowHtml = `
            <tr onclick="showProductDetailModal(${p.productId})" style="cursor: pointer;">
                <td class="text-center align-middle">${index + 1}</td>
                <td class="text-center align-middle">
                    <img src="${p.imageUrl}" class="product-thumb-img" onerror="this.src='/img/placeholder.png'" />
                </td>
                <td class="align-middle">
                    <div class="fw-bold text-dark">${p.productName} ${promotionBadge}</div>
                    <small class="text-muted" style="font-size: 12px;">Mã: ${p.productId}</small>
                </td>
                <td class="text-end align-middle fw-bold text-primary">
                    ${(p.price || 0).toLocaleString('vi-VN')}đ
                </td>
                
                <!-- SỬA CỘT SỐ LƯỢNG -->
                <td class="text-center align-middle">
                    ${stockDisplay}
                </td>
                
                <td class="text-center align-middle">
                    <button class="btn btn-sm btn-outline-primary"><i class="fas fa-plus"></i></button>
                </td>
            </tr>`;
        productsList.append(rowHtml);
    });
}
function renderOrderTabs() {
    const tabsContainer = $('#order-tabs').empty();
    orders.forEach((order, index) => {
        // Logic style mới
        const isActive = index === currentOrderIndex;
        const btnClass = isActive ? 'btn-primary text-white' : 'btn-light text-dark bg-white border';
        const statusText = order.status === 1 ? ' <span class="badge bg-warning text-dark" style="font-size:9px">Chờ TT</span>' : '';

        const tab = $(`<button class="btn btn-sm ${btnClass} me-1" style="border-bottom:none; border-radius: 5px 5px 0 0;">
                          Hóa đơn ${order.displayId}${statusText}
                       </button>`);

        tab.on('click', () => switchToOrder(index));
        tabsContainer.append(tab);
    });
}


function renderCart() {
    const cartBody = $('#cart-items').empty();

    // Nếu chưa chọn đơn hàng
    if (currentOrderIndex < 0 || !orders[currentOrderIndex]) {
        $('#subtotal-amount, #discount-amount, #total-amount, #change-due-amount').text('0');
        return cartBody.html('<tr><td colspan="5" class="text-center text-muted py-3">Vui lòng chọn hóa đơn</td></tr>');
    }

    const currentOrder = orders[currentOrderIndex];
    const cart = currentOrder.items;
    let subTotal = 0;

    if (cart.length === 0) {
        cartBody.html('<tr><td colspan="5" class="text-center text-muted py-3">Giỏ hàng trống</td></tr>');
    } else {
        cart.forEach((item, index) => {
            const itemTotal = item.quantity * item.unitPrice;
            subTotal += itemTotal;

            const row = `<tr>
                <td>
                    <!-- Bỏ max-width và text-truncate để chữ tự xuống dòng -->
                    <div class="fw-bold" style="white-space: normal; line-height: 1.3;">
                        ${item.name}
                    </div>
                </td>
                <td class="text-center">
                    <input type="number" value="${item.quantity}" min="1" max="${item.stock}"
                           class="form-control form-control-sm text-center px-1" 
                           style="width: 50px; margin: 0 auto;" 
                           onchange="updateQuantity(${index}, this.value)">
                </td>
                <td class="text-end">${Math.round(item.unitPrice).toLocaleString('vi-VN')}</td>
                <td class="text-end fw-bold">${Math.round(itemTotal).toLocaleString('vi-VN')}</td>
                <td class="text-center">
                    <i class="fas fa-times text-danger" style="cursor:pointer" onclick="removeItem(${index})"></i>
                </td>
             </tr>`;
            cartBody.append(row);
        });
    }

    // --- PHẦN TÍNH TOÁN (LOGIC GIỮ NGUYÊN) ---
    const appliedPromotionsContainer = $('#applied-promotions-list').empty();
    let totalAfterAutoPromo = subTotal;
    const autoPromoDiscount = 0;

    appliedPromotionsContainer.html('<span class="text-muted">Không áp dụng.</span>');
    let voucherDiscount = 0;
    if (currentOrder.voucher && currentOrder.voucher.id) {
        const voucherAsPromo = {
            loaiGiamGia: currentOrder.voucher.loaiGiamGia,
            giaTriGiamGia: currentOrder.voucher.giaTriGiamGia
        };
        const totalAfterVoucher = calculateDiscountedPrice(totalAfterAutoPromo, voucherAsPromo);
        voucherDiscount = totalAfterAutoPromo - totalAfterVoucher;
    }

    const roundedSubTotal = Math.round(subTotal);
    const roundedTotalDiscount = Math.round(autoPromoDiscount + voucherDiscount);
    const roundedFinalTotal = roundedSubTotal - roundedTotalDiscount;

    $('#subtotal-amount').text(roundedSubTotal.toLocaleString('vi-VN'));
    $('#discount-amount').text(roundedTotalDiscount.toLocaleString('vi-VN'));
    $('#total-amount').text(Math.max(0, roundedFinalTotal).toLocaleString('vi-VN'));

    // === LOGIC TÍNH TIỀN KHÁCH ĐƯA ===
    let changeDue = 0;
    const paymentMethod = $('#payment-method-select').val();
    const cashInputStr = $('#cash-received-input').val().trim(); // Lấy chuỗi nhập vào
    const cashReceived = parseCurrency(cashInputStr); // Chuyển thành số

    // Mặc định ẩn dòng "Khách thiếu" và Reset màu sắc
    $('#shortage-section').hide();
    $('#change-due-section').show();
    $('#change-due-label').text("Tiền thừa:");
    $('#change-due-amount').removeClass('text-danger').addClass('text-success');

    if (paymentMethod === 'Tiền mặt') {
        // Chỉ tính toán khi có nhập dữ liệu
        if (cashInputStr !== "") {
            if (cashReceived >= roundedFinalTotal) {
                // Trường hợp ĐỦ hoặc THỪA tiền
                changeDue = cashReceived - roundedFinalTotal;
                $('#change-due-amount').text(Math.round(changeDue).toLocaleString('vi-VN'));
            } else {
                // Trường hợp THIẾU tiền
                const shortage = roundedFinalTotal - cashReceived;

                // Đổi giao diện sang báo thiếu
                $('#change-due-label').text("Khách còn thiếu:");
                $('#change-due-amount').text(Math.round(shortage).toLocaleString('vi-VN'));
                $('#change-due-amount').removeClass('text-success').addClass('text-danger');
            }
        } else {
            // Nếu không nhập gì -> Tiền thừa = 0 (Coi như đưa đủ)
            $('#change-due-amount').text('0');
        }
    } else {
        // Nếu là Chuyển khoản -> Tiền thừa = 0
        $('#change-due-amount').text('0');
    }
}
async function switchToOrder(index) {
    // 1. Kiểm tra chỉ mục hợp lệ
    if (index < 0 || index >= orders.length) return;

    currentOrderIndex = index;
    const currentOrder = orders[currentOrderIndex];

    // =========================================================
    // LOGIC ẨN HIỆN GIAO DIỆN
    // =========================================================
    if (!currentOrder) {
        // Nếu không có hóa đơn -> Ẩn chi tiết, Hiện màn hình chào
        $('#order-section-wrapper').hide();
        $('#initial-order-prompt').show();
        return;
    } else {
        // Nếu CÓ hóa đơn -> Ẩn màn hình chào
        $('#initial-order-prompt').hide();

        // Hiện chi tiết hóa đơn (dùng css để ép hiển thị flex)
        $('#order-section-wrapper').css('display', 'flex').show();
    }

    // 2. Tải lại dữ liệu khuyến mãi
    await refreshPromotionsData();

    // 3. Render lại các thông tin
    renderOrderTabs();
    $('#current-order-title').text(`Hóa đơn ${currentOrder.displayId}`);
    $('#customer-select').val(currentOrder.customerId || "");
    $('#payment-method-select').val(currentOrder.paymentMethod || "Tiền mặt");

    const voucherSelect = $('#voucher-code-select');
    const messageContainer = $('#voucher-status-message');

    if (currentOrder.voucher && currentOrder.voucher.id) {
        // NẾU CÓ VOUCHER
        if (!voucherSelect.find(`option[value='${currentOrder.voucher.code}']`).length) {
            voucherSelect.append(new Option(currentOrder.voucher.code, currentOrder.voucher.code, true, true));
        }
        voucherSelect.val(currentOrder.voucher.code).trigger('change');
        messageContainer.text(`Đã áp dụng mã '${currentOrder.voucher.code}'`).removeClass('text-danger').addClass('text-success');

        // ==> Logic Ẩn/Hiện nút
        $('#apply-voucher-btn').hide();       // Ẩn nút Áp dụng
        $('#remove-voucher-btn').show();      // Hiện nút Hủy
        voucherSelect.prop('disabled', true); // Khóa ô chọn lại cho đỡ nhầm
    } else {
        // NẾU KHÔNG CÓ VOUCHER
        voucherSelect.val(null).trigger('change');
        messageContainer.text('').removeClass('text-success text-danger');

        // ==> Logic Ẩn/Hiện nút
        $('#apply-voucher-btn').show();       // Hiện nút Áp dụng
        $('#remove-voucher-btn').hide();      // Ẩn nút Hủy
        voucherSelect.prop('disabled', false); // Mở khóa ô chọn
    }

    toggleCashPaymentSection();
    renderCart();
}
function toggleCashPaymentSection() {
    const isCash = $('#payment-method-select').val() === 'Tiền mặt';
    $('#cash-payment-section, #change-due-section').toggle(isCash);
    if (!isCash) $('#cash-received-input').val('');
    renderCart();
}

function populateDropdowns() {
    const customerSelect = $('#customer-select').empty().append('<option value="">Khách vãng lai</option>');
    customersData.forEach(c => {
        customerSelect.append(`<option value="${c.maKhachHang}">${c.hoTen} - ${c.soDienThoai}</option>`);
    });
}


/** Hiển thị modal chi tiết sản phẩm. */
async function showProductDetailModal(productId) {
    const modalBody = $('#productDetailBody');
    modalBody.html('<p class="text-center">Đang tải chi tiết...</p>');
    $('#productDetailModal').modal('show');
    try {
        const response = await fetch(`${apiUrls.getProductDetails}?productId=${productId}`);
        const details = await response.json();
        modalBody.empty();
        modalBody.attr('data-product-id', details.productId);

        let content = `<h4>${details.productName}</h4>`;
        if (!details.colors || details.colors.length === 0) {
            content += '<p>Sản phẩm này hiện đã hết hàng.</p>';
        } else {
            details.colors.forEach(color => {
                content += `<div class="color-group"><h5>Màu: ${color.colorName} <span class="color-swatch" style="background-color:${color.colorCode || '#ccc'};"></span></h5><div class="size-options">`;
                color.sizes.forEach(size => {
                    let priceDisplay = `${size.price.toLocaleString('vi-VN')}đ`;
                    if (size.originalPrice && size.originalPrice > size.price) {
                        priceDisplay = `<span class="text-danger">${priceDisplay}</span> <s class="text-muted">${size.originalPrice.toLocaleString('vi-VN')}đ</s>`;
                    }
                    // =========================================================================
                    // === THAY ĐỔI QUAN TRỌNG Ở ĐÂY: Thêm data-product-name vào thẻ input   ===
                    // =========================================================================
                    content += `<div class="size-option">
                                    <input type="radio" name="product_detail_id" 
                                           id="detail_${size.productDetailId}" 
                                           value="${size.productDetailId}" 
                                           data-price="${size.price}" 
                                           data-size-name="${size.sizeName}" 
                                           data-color-name="${color.colorName}"  
                                           data-stock="${size.stock}"
                                           data-product-name="${details.productName}" /> 
                                    <label for="detail_${size.productDetailId}">${size.sizeName} (${size.stock} có sẵn) - ${priceDisplay}</label>
                                </div>`;
                });
                content += `</div></div>`;
            });
        }
        modalBody.html(content);
    } catch (error) {
        console.error('Lỗi khi lấy chi tiết sản phẩm:', error);
        modalBody.html('<p class="text-danger">Không thể tải chi tiết sản phẩm.</p>');
    }
}


// =================================================================
// HÀNH ĐỘNG TRÊN GIỎ HÀNG (CART ACTIONS)
// =================================================================

/** Thêm sản phẩm vào giỏ hàng từ modal. */
function addProductToCartFromModal() {
    const selectedDetailRadio = $('input[name="product_detail_id"]:checked');
    if (!selectedDetailRadio.length) {
        alert('Vui lòng chọn một phiên bản sản phẩm.');
        return;
    }

    // 1. Lấy thông tin
    const productDetailId = parseInt(selectedDetailRadio.val());
    const price = parseFloat(selectedDetailRadio.data('price'));
    const sizeName = selectedDetailRadio.data('size-name');
    const colorName = selectedDetailRadio.data('color-name');
    const stock = parseInt(selectedDetailRadio.data('stock'));
    const productName = selectedDetailRadio.data('product-name');

    // === LẤY ID SẢN PHẨM CHA (QUAN TRỌNG) ===
    // ID này được lưu vào modal khi mở lên
    const parentProductId = parseInt($('#productDetailBody').data('product-id'));
    // ========================================

    if (!productName) {
        alert("Lỗi nghiêm trọng: Không thể xác định tên sản phẩm.");
        return;
    }

    const cart = orders[currentOrderIndex].items;
    const existingItem = cart.find(item => item.productDetailId === productDetailId);

    

    if (existingItem) {
        if (existingItem.quantity + 1 > existingItem.stock) {
            alert(`Không thể thêm. Số lượng tồn kho không đủ.`);
            return;
        }
      
        existingItem.quantity++;
    } else {
        cart.push({
            productDetailId,
            name: `${productName} (${colorName} - ${sizeName})`,
            quantity: 1,
            unitPrice: price,
            stock: stock,
            productId: parentProductId // === LƯU ID CHA VÀO GIỎ ===
        });
    }

    // === CẬP NHẬT TỒN KHO BÊN NGOÀI (TRỪ ĐI 1) ===
    updateLocalProductStock(parentProductId, -1);
    // =============================================

    renderCart();
    $('#productDetailModal').modal('hide');
}
/** Cập nhật số lượng sản phẩm trong giỏ hàng. */
function updateQuantity(itemIndex, newQuantity) {
    let qty = parseInt(newQuantity);
    const item = orders[currentOrderIndex].items[itemIndex];
    const availableStock = item.stock;

    if (qty > availableStock) {
        alert(`Số lượng tồn kho không đủ.`);
        qty = availableStock;
        $(`#cart-items tr:eq(${itemIndex}) input[type=number]`).val(qty);
    }
  

    if (qty > 0) {
        const oldQty = item.quantity;
        const diff = oldQty - qty;

        if (item.productId) {
            updateLocalProductStock(item.productId, diff);
        }

        item.quantity = qty;
    } else {
        item.quantity = 1;
        // Hoặc xử lý xóa nếu qty = 0 tùy bạn, ở đây giữ logic cũ là reset về 1
        $(`#cart-items tr:eq(${itemIndex}) input[type=number]`).val(1);
    }
    renderCart();
}

/** Xóa một sản phẩm khỏi giỏ hàng. */
function removeItem(itemIndex) {
    const item = orders[currentOrderIndex].items[itemIndex];

    // === CẬP NHẬT TỒN KHO BÊN NGOÀI (CỘNG TRẢ LẠI) ===
    if (item.productId) {
        updateLocalProductStock(item.productId, item.quantity);
    }
    // =================================================

    orders[currentOrderIndex].items.splice(itemIndex, 1);

    if (orders[currentOrderIndex].items.length === 0) {
        orders[currentOrderIndex].status = 0;
        renderOrderTabs();
    }

    renderCart();
}

/** Lọc danh sách sản phẩm theo từ khóa. */
function filterProducts(keyword) {
    const lowerKeyword = keyword.toLowerCase();
    const filtered = productsData.filter(p => p.productName.toLowerCase().includes(lowerKeyword));
    renderProducts(filtered);
}

/** Xóa một hóa đơn khỏi giao diện (sau khi thanh toán hoặc hủy). */
function removeOrderFromUI(orderId) {
    const orderIndex = orders.findIndex(o => o.id === orderId);
    if (orderIndex > -1) {
        orders.splice(orderIndex, 1); // Xóa khỏi mảng

        // === THÊM ĐOẠN NÀY: ĐÁNH SỐ LẠI TỪ ĐẦU ===
        orders.forEach((order, index) => {
            order.displayId = index + 1;
        });
        // =========================================
    }

    if (orders.length === 0) {
        currentOrderIndex = -1;
        $('#order-section-wrapper').hide();
        $('#initial-order-prompt').show();
        renderOrderTabs();
        renderCart(); // Xóa trắng giỏ hàng
    } else {
        // Chuyển về tab liền trước (hoặc tab đầu tiên nếu xóa tab 1)
        const newIndex = Math.max(0, orderIndex - 1);
        switchToOrder(newIndex);
    }
}


// =================================================================
// XỬ LÝ MÃ QR (QR CODE SCANNING)
// =================================================================

/** Khởi động camera để quét mã QR. */
function startQrScanner() {
    const config = {
        fps: 60,
        qrbox: (viewfinderWidth, viewfinderHeight) => {
            let minEdge = Math.min(viewfinderWidth, viewfinderHeight);
            let qrboxSize = Math.floor(minEdge * 0.8);
            return { width: qrboxSize, height: qrboxSize };
        },
        experimentalFeatures: { useBarCodeDetectorIfSupported: true },
        rememberLastUsedCamera: true
    };
    if (!html5QrcodeScanner) {
        html5QrcodeScanner = new Html5Qrcode("qr-reader", false);
    }
    $('#qrScannerModal').modal('show');
    html5QrcodeScanner.start(
        { facingMode: "environment" }, config, onScanSuccess, onScanFailure
    ).catch(() => {
        html5QrcodeScanner.start({ facingMode: "user" }, config, onScanSuccess, onScanFailure)
            .catch(() => {
                $('#qr-scan-status').text("Không tìm thấy camera. Hãy thử tải ảnh lên.");
            });
    });
}

/** Dừng camera. */
function stopQrScanner() {
    if (html5QrcodeScanner && html5QrcodeScanner.getState() === 2) { // 2 = SCANNING
        html5QrcodeScanner.stop().catch(err => console.error("Lỗi khi dừng scanner:", err));
    }
}

/** Xử lý khi quét mã thành công. */
function onScanSuccess(decodedText, decodedResult) {
    stopQrScanner();
    $('#qrScannerModal').modal('hide');
    processScannedProduct(decodedText);
}

/** Xử lý khi quét thất bại (không cần làm gì). */
function onScanFailure(error) {
    // Thư viện sẽ tự động thử lại
}

/** Gửi mã QR đã quét lên server để lấy thông tin sản phẩm. */
async function processScannedProduct(scannedId) {
    const productDetailId = parseInt(scannedId);
    if (isNaN(productDetailId)) {
        console.error("Mã QR không hợp lệ:", scannedId);
        return;
    }
    // Tự động tạo hóa đơn mới nếu chưa có hóa đơn nào
    if (currentOrderIndex < 0) {
        await startFirstOrder();
    }
    // Nếu vẫn không tạo được hóa đơn thì dừng lại
    if (currentOrderIndex < 0) {
        alert("Không thể thêm sản phẩm, vui lòng tạo hóa đơn trước.");
        return;
    }
    try {
        const response = await fetch(`${apiUrls.getProductByQr}?productDetailId=${productDetailId}`);
        if (!response.ok) {
            const errorResult = await response.json();
            throw new Error(errorResult.message || 'Không tìm thấy sản phẩm.');
        }

        const productData = await response.json();
        const cart = orders[currentOrderIndex].items;
        const existingItem = cart.find(item => item.productDetailId === productData.productDetailId);

        // Lấy số lượng hiện có trong giỏ hàng để kiểm tra tồn kho
        const currentQtyInCart = existingItem ? existingItem.quantity : 0;
        if (currentQtyInCart + 1 > productData.stock) {
            alert(`Không thể thêm. Số lượng tồn kho không đủ (chỉ còn ${productData.stock}).`);
            return;
        }

        if (existingItem) {
           
            existingItem.quantity++;
        } else {
          
            cart.push({
                productDetailId: productData.productDetailId,
                name: productData.name,
                quantity: 1,
                unitPrice: productData.unitPrice,
                stock: productData.stock // Quan trọng: Lưu lại số lượng tồn kho
            });
        }
        renderCart();
    } catch (error) {
        console.error('Lỗi khi xử lý sản phẩm từ mã QR:', error);
        alert(`Lỗi: ${error.message}`);
    }
}

/** Xử lý khi người dùng tải file ảnh mã QR lên. */
function handleQrFileUpload(e) {
    if (e.target.files.length === 0) return;
    const imageFile = e.target.files[0];
    if (!html5QrcodeScanner) {
        html5QrcodeScanner = new Html5Qrcode("qr-reader", false);
    }
    html5QrcodeScanner.scanFile(imageFile, true)
        .then(decodedText => onScanSuccess(decodedText, null))
        .catch(err => {
            $('#qr-scan-status').text(`Lỗi khi quét file ảnh: ${err}`);
        });
}


// =================================================================
// HÀM TIỆN ÍCH (UTILITY FUNCTIONS)
// =================================================================

/** Chuyển đổi chuỗi tiền tệ (vd: "100.000đ") thành số. */
async function initializeVoucherSelect() {
    try {
        const response = await fetch(apiUrls.getVouchers);
        const vouchers = await response.json();
        $('#voucher-code-select').select2({
            placeholder: "Nhập hoặc chọn mã giảm giá",
            tags: true,
            data: vouchers,
            // Thư viện select2 cần được thêm vào layout để có theme đẹp hơn
        });
    } catch (error) {
        console.error("Lỗi khi tải danh sách voucher:", error);
    }
}

async function applyVoucher() {
    const currentOrder = orders[currentOrderIndex];
    if (!currentOrder) return;
    const voucherCode = $('#voucher-code-select').val();
    if (!voucherCode) return alert("Vui lòng nhập mã giảm giá.");
    const subTotal = currentOrder.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);

    try {
        const response = await fetch(apiUrls.applyVoucher, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ voucherCode: voucherCode, subTotal: subTotal })
        });
        const result = await response.json();
        const messageContainer = $('#voucher-status-message');

        if (result.success) {
            // Lưu thông tin voucher
            currentOrder.voucher = {
                id: result.voucherId,
                code: voucherCode.toUpperCase(),
                loaiGiamGia: result.loaiGiamGia,
                giaTriGiamGia: result.giaTriGiamGia
            };
            messageContainer.text(result.message).removeClass('text-danger').addClass('text-success');

            // ==> THÊM: Cập nhật giao diện nút
            $('#apply-voucher-btn').hide();
            $('#remove-voucher-btn').show();
            $('#voucher-code-select').prop('disabled', true);
        } else {
            currentOrder.voucher = { id: null, code: '', loaiGiamGia: null, giaTriGiamGia: 0 };
            messageContainer.text(result.message).removeClass('text-success').addClass('text-danger');
        }
        renderCart();
    } catch (error) {
        console.error("Lỗi khi áp dụng voucher:", error);
    }
}

// =================================================================
// HÀM TIỆN ÍCH (UTILITY FUNCTIONS)
// =================================================================
function parseCurrency(value) {
    if (!value || typeof value !== 'string') return 0;
    return parseFloat(value.replace(/[^\d]/g, '')) || 0;
}

function calculateDiscountedPrice(originalPrice, promotion) {
    if (!promotion) return originalPrice;
    let discount = (promotion.loaiGiamGia === 'PhanTram')
        ? originalPrice * (promotion.giaTriGiamGia / 100)
        : promotion.giaTriGiamGia;
    return Math.max(0, originalPrice - discount);
}

async function refreshPromotionsData() {
    try {
        const response = await fetch(apiUrls.getPromotions);
        if (!response.ok) return console.error("Lỗi tải lại khuyến mãi.");
        promotionsData = await response.json();
        renderCart();
    } catch (error) {
        console.error('Lỗi tải lại dữ liệu khuyến mãi:', error);
    }
}
function removeVoucher() {
    const currentOrder = orders[currentOrderIndex];
    if (!currentOrder) return;

    // 1. Reset dữ liệu voucher về rỗng
    currentOrder.voucher = { id: null, code: '', loaiGiamGia: null, giaTriGiamGia: 0 };

    // 2. Xóa chọn trong Select2 và xóa thông báo
    $('#voucher-code-select').val(null).trigger('change');
    $('#voucher-status-message').text('').removeClass('text-success text-danger');

    // 3. Ẩn nút Hủy, Hiện nút Áp dụng
    $('#remove-voucher-btn').hide();
    $('#apply-voucher-btn').show();
    $('#voucher-code-select').prop('disabled', false); // Mở khóa ô nhập

    // 4. Tính lại tiền
    renderCart();
}
/**
 * Cập nhật số lượng hiển thị trên bảng sản phẩm (Không gọi API)
 * @param {number} productId - ID sản phẩm cha
 * @param {number} changeAmount - Số lượng thay đổi (Số dương để cộng, số âm để trừ)
 */
function updateLocalProductStock(productId, changeAmount) {
    const product = productsData.find(p => p.productId === productId);
    if (product) {
        // Cộng trừ số lượng
        product.totalStock = (product.totalStock || 0) + changeAmount;

        // Vẽ lại bảng sản phẩm để hiển thị số mới
        renderProducts(productsData);
    }
}