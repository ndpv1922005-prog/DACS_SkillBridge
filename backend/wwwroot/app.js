(function () {
  var dict = {
    en: {
      brandSubtitle: "Marketplace MVP",
      navMarketplace: "Marketplace",
      navDashboard: "Dashboard",
      navChat: "Chat",
      navCall: "Video Call",
      heroEyebrow: "Production-style learning workflow",
      heroTitle: "Discover, book, pay, chat, and join sessions.",
      languageLabel: "Language",
      emailLabel: "Email",
      passwordLabel: "Password",
      loginButton: "Login",
      statBookings: "Total bookings",
      statHeld: "Escrow held",
      statPaid: "Paid sessions",
      statOnline: "Online teachers",
      marketplaceEyebrow: "Teacher Marketplace",
      marketplaceTitle: "Find the right mentor",
      searchPlaceholder: "Search skill, teacher, topic",
      modeAll: "All modes",
      modeOnline: "Online",
      modeOffline: "Offline",
      modeHybrid: "Hybrid",
      bookingEyebrow: "Booking System",
      bookingTitle: "Session flow",
      refreshButton: "Refresh",
      chatEyebrow: "Realtime Chat",
      selectTeacher: "Select a teacher",
      offline: "Offline",
      online: "Online",
      messagePlaceholder: "Write a message",
      sendButton: "Send",
      cameraPreview: "Camera Preview",
      paidBookingRequired: "Paid booking required",
      mentor: "Mentor",
      cameraButton: "Camera",
      micButton: "Microphone",
      endButton: "End",
      rating: "rating",
      perSession: "/ session",
      bookSession: "Book session",
      openChat: "Open chat",
      noBookings: "No bookings yet. Choose a teacher to create the first request.",
      teacherFallback: "Teacher",
      confirm: "Confirm",
      reject: "Reject",
      pay: "Pay",
      joinCall: "Join call",
      complete: "Complete",
      loginFirst: "Login first",
      loggedIn: "Logged in as {name}",
      bookingCreated: "Booking request created",
      selectTeacherFirst: "Select a teacher first",
      connectedCall: "Connected to session room",
      cameraToggled: "Camera toggled",
      micToggled: "Microphone toggled",
      callEnded: "Call ended",
      requestFailed: "Request failed",
      roles: { 0: "Student", 1: "Teacher", Student: "Student", Teacher: "Teacher" },
      statuses: { Pending: "Pending", Confirmed: "Confirmed", Paid: "Paid", InProgress: "In progress", Completed: "Completed", Cancelled: "Cancelled", Rejected: "Rejected", Refunded: "Refunded" },
      modes: { 0: "Online", 1: "Offline", 2: "Hybrid", Online: "Online", Offline: "Offline", Hybrid: "Hybrid" }
    },
    vi: {
      brandSubtitle: "MVP ch\u1ee3 k\u1ef9 n\u0103ng",
      navMarketplace: "T\u00ecm gi\u00e1o vi\u00ean",
      navDashboard: "B\u1ea3ng \u0111i\u1ec1u khi\u1ec3n",
      navChat: "Tr\u00f2 chuy\u1ec7n",
      navCall: "G\u1ecdi video",
      heroEyebrow: "Quy tr\u00ecnh h\u1ecdc t\u1eadp ki\u1ec3u s\u1ea3n ph\u1ea9m th\u1eadt",
      heroTitle: "T\u00ecm gi\u00e1o vi\u00ean, \u0111\u1eb7t l\u1ecbch, thanh to\u00e1n, tr\u00f2 chuy\u1ec7n v\u00e0 v\u00e0o bu\u1ed5i h\u1ecdc.",
      languageLabel: "Ng\u00f4n ng\u1eef",
      emailLabel: "Email",
      passwordLabel: "M\u1eadt kh\u1ea9u",
      loginButton: "\u0110\u0103ng nh\u1eadp",
      statBookings: "T\u1ed5ng l\u1ecbch \u0111\u1eb7t",
      statHeld: "Ti\u1ec1n \u0111ang gi\u1eef",
      statPaid: "Bu\u1ed5i \u0111\u00e3 thanh to\u00e1n",
      statOnline: "Gi\u00e1o vi\u00ean tr\u1ef1c tuy\u1ebfn",
      marketplaceEyebrow: "Ch\u1ee3 gi\u00e1o vi\u00ean",
      marketplaceTitle: "T\u00ecm \u0111\u00fang ng\u01b0\u1eddi h\u01b0\u1edbng d\u1eabn",
      searchPlaceholder: "T\u00ecm k\u1ef9 n\u0103ng, gi\u00e1o vi\u00ean, ch\u1ee7 \u0111\u1ec1",
      modeAll: "T\u1ea5t c\u1ea3 h\u00ecnh th\u1ee9c",
      modeOnline: "Tr\u1ef1c tuy\u1ebfn",
      modeOffline: "Tr\u1ef1c ti\u1ebfp",
      modeHybrid: "K\u1ebft h\u1ee3p",
      bookingEyebrow: "H\u1ec7 th\u1ed1ng \u0111\u1eb7t l\u1ecbch",
      bookingTitle: "Lu\u1ed3ng bu\u1ed5i h\u1ecdc",
      refreshButton: "L\u00e0m m\u1edbi",
      chatEyebrow: "Tr\u00f2 chuy\u1ec7n th\u1eddi gian th\u1ef1c",
      selectTeacher: "Ch\u1ecdn m\u1ed9t gi\u00e1o vi\u00ean",
      offline: "Ngo\u1ea1i tuy\u1ebfn",
      online: "Tr\u1ef1c tuy\u1ebfn",
      messagePlaceholder: "Nh\u1eadp tin nh\u1eafn",
      sendButton: "G\u1eedi",
      cameraPreview: "Xem tr\u01b0\u1edbc camera",
      paidBookingRequired: "C\u1ea7n l\u1ecbch \u0111\u00e3 thanh to\u00e1n",
      mentor: "Ng\u01b0\u1eddi h\u01b0\u1edbng d\u1eabn",
      cameraButton: "Camera",
      micButton: "Micro",
      endButton: "K\u1ebft th\u00fac",
      rating: "\u0111\u00e1nh gi\u00e1",
      perSession: "/ bu\u1ed5i",
      bookSession: "\u0110\u1eb7t l\u1ecbch",
      openChat: "M\u1edf chat",
      noBookings: "Ch\u01b0a c\u00f3 l\u1ecbch \u0111\u1eb7t. H\u00e3y ch\u1ecdn gi\u00e1o vi\u00ean \u0111\u1ec3 t\u1ea1o y\u00eau c\u1ea7u \u0111\u1ea7u ti\u00ean.",
      teacherFallback: "Gi\u00e1o vi\u00ean",
      confirm: "X\u00e1c nh\u1eadn",
      reject: "T\u1eeb ch\u1ed1i",
      pay: "Thanh to\u00e1n",
      joinCall: "V\u00e0o cu\u1ed9c g\u1ecdi",
      complete: "Ho\u00e0n t\u1ea5t",
      loginFirst: "Vui l\u00f2ng \u0111\u0103ng nh\u1eadp tr\u01b0\u1edbc",
      loggedIn: "\u0110\u00e3 \u0111\u0103ng nh\u1eadp v\u1edbi t\u00e0i kho\u1ea3n {name}",
      bookingCreated: "\u0110\u00e3 t\u1ea1o y\u00eau c\u1ea7u \u0111\u1eb7t l\u1ecbch",
      selectTeacherFirst: "Vui l\u00f2ng ch\u1ecdn gi\u00e1o vi\u00ean tr\u01b0\u1edbc",
      connectedCall: "\u0110\u00e3 k\u1ebft n\u1ed1i v\u00e0o ph\u00f2ng h\u1ecdc",
      cameraToggled: "\u0110\u00e3 b\u1eadt/t\u1eaft camera",
      micToggled: "\u0110\u00e3 b\u1eadt/t\u1eaft micro",
      callEnded: "Cu\u1ed9c g\u1ecdi \u0111\u00e3 k\u1ebft th\u00fac",
      requestFailed: "Y\u00eau c\u1ea7u th\u1ea5t b\u1ea1i",
      roles: { 0: "H\u1ecdc vi\u00ean", 1: "Gi\u00e1o vi\u00ean", Student: "H\u1ecdc vi\u00ean", Teacher: "Gi\u00e1o vi\u00ean" },
      statuses: { Pending: "Ch\u1edd duy\u1ec7t", Confirmed: "\u0110\u00e3 x\u00e1c nh\u1eadn", Paid: "\u0110\u00e3 thanh to\u00e1n", InProgress: "\u0110ang h\u1ecdc", Completed: "\u0110\u00e3 ho\u00e0n th\u00e0nh", Cancelled: "\u0110\u00e3 h\u1ee7y", Rejected: "\u0110\u00e3 t\u1eeb ch\u1ed1i", Refunded: "\u0110\u00e3 ho\u00e0n ti\u1ec1n" },
      modes: { 0: "Tr\u1ef1c tuy\u1ebfn", 1: "Tr\u1ef1c ti\u1ebfp", 2: "K\u1ebft h\u1ee3p", Online: "Tr\u1ef1c tuy\u1ebfn", Offline: "Tr\u1ef1c ti\u1ebfp", Hybrid: "K\u1ebft h\u1ee3p" }
    }
  };

  var state = {
    lang: localStorage.getItem("skillbridge-lang") || "vi",
    user: null,
    teachers: [],
    selectedTeacher: null,
    bookings: []
  };

  function el(id) { return document.getElementById(id); }
  function text(key) { return (dict[state.lang] && dict[state.lang][key]) || dict.en[key] || key; }
  function json(url, options) {
    return fetch(url, options).then(function (res) {
      return res.json().catch(function () { return null; }).then(function (data) {
        if (!res.ok) throw new Error((data && data.error) || text("requestFailed"));
        return data;
      });
    });
  }
  function post(url, body) {
    return json(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  }
  function money(value) { return new Intl.NumberFormat("vi-VN").format(value) + "\u0111"; }
  function when(value) {
    var locale = state.lang === "vi" ? "vi-VN" : "en-US";
    return new Intl.DateTimeFormat(locale, { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
  }
  function toast(message) {
    el("toast").textContent = message;
    el("toast").classList.add("show");
    window.setTimeout(function () { el("toast").classList.remove("show"); }, 2600);
  }
  function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, function (char) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" }[char];
    });
  }

  function applyLanguage() {
    document.documentElement.lang = state.lang;
    el("language").value = state.lang;

    Array.prototype.forEach.call(document.querySelectorAll("[data-i18n]"), function (node) {
      node.textContent = text(node.getAttribute("data-i18n"));
    });
    Array.prototype.forEach.call(document.querySelectorAll("[data-i18n-attr]"), function (node) {
      var pairs = node.getAttribute("data-i18n-attr").split(";");
      pairs.forEach(function (pair) {
        var parts = pair.split(":");
        node.setAttribute(parts[0], text(parts[1]));
      });
    });

    if (state.user) renderUser();
    renderTeachers();
    renderBookings();
    renderStats();
    if (!state.selectedTeacher) {
      el("chatTitle").textContent = text("selectTeacher");
      el("chatStatus").textContent = text("offline");
    } else {
      el("chatStatus").textContent = state.selectedTeacher.isOnline ? text("online") : text("offline");
    }
  }

  function setLanguage(lang) {
    state.lang = lang;
    localStorage.setItem("skillbridge-lang", lang);
    applyLanguage();
  }

  function login() {
    post("/api/auth/login", {
      email: el("email").value,
      password: el("password").value
    }).then(function (user) {
      state.user = user;
      localStorage.setItem("skillbridge-user", JSON.stringify(user));
      renderUser();
      loadTeachers();
      loadBookings();
      toast(text("loggedIn").replace("{name}", user.name));
    }).catch(function (err) {
      toast(err.message);
    });
  }

  function renderUser() {
    var role = text("roles")[state.user.role] || state.user.role;
    el("currentUser").innerHTML =
      '<img class="avatar" src="' + state.user.avatarUrl + '" alt="">' +
      '<div><strong>' + escapeHtml(state.user.name) + '</strong><span>' + role + '</span></div>';
  }

  function loadTeachers() {
    var params = new URLSearchParams();
    if (el("search").value.trim()) params.set("query", el("search").value.trim());
    if (el("mode").value) params.set("mode", el("mode").value);
    json("/api/teachers?" + params.toString()).then(function (teachers) {
      state.teachers = teachers;
      renderTeachers();
      renderStats();
    }).catch(function (err) { toast(err.message); });
  }

  function renderTeachers() {
    el("teacherGrid").innerHTML = state.teachers.map(function (teacher) {
      var mode = text("modes")[teacher.teachingMode] || teacher.teachingMode;
      var online = teacher.isOnline ? text("online") : text("offline");
      return '<article class="teacher-card">' +
        '<div class="teacher-head"><img class="avatar" src="' + teacher.avatarUrl + '" alt=""><div><strong>' + escapeHtml(teacher.name) + '</strong><p>' + escapeHtml(teacher.skill) + '</p></div></div>' +
        '<p>' + escapeHtml(teacher.description) + '</p>' +
        '<div class="teacher-meta"><span class="badge">' + mode + '</span><span class="badge">' + teacher.rating + ' ' + text("rating") + '</span><span class="badge">' + online + '</span></div>' +
        '<strong>' + money(teacher.pricePerSession) + ' ' + text("perSession") + '</strong>' +
        '<button data-book="' + teacher.id + '">' + text("bookSession") + '</button>' +
        '<button class="secondary" data-chat="' + teacher.id + '">' + text("openChat") + '</button>' +
        '</article>';
    }).join("");
  }

  function bookTeacher(teacherId) {
    if (!state.user) return toast(text("loginFirst"));
    var start = new Date(Date.now() + 24 * 60 * 60 * 1000);
    start.setMinutes(0, 0, 0);
    post("/api/bookings", {
      studentId: state.user.id,
      teacherProfileId: teacherId,
      startTime: start.toISOString(),
      durationMinutes: 60
    }).then(function () {
      loadBookings();
      toast(text("bookingCreated"));
    }).catch(function (err) { toast(err.message); });
  }

  function loadBookings() {
    if (!state.user) return;
    json("/api/bookings?userId=" + state.user.id).then(function (bookings) {
      state.bookings = bookings;
      renderBookings();
      renderStats();
    }).catch(function (err) { toast(err.message); });
  }

  function renderBookings() {
    var list = el("bookingList");
    if (!state.bookings.length) {
      list.innerHTML = '<div class="empty">' + text("noBookings") + '</div>';
      return;
    }
    list.innerHTML = state.bookings.map(function (booking) {
      var teacher = state.teachers.find(function (item) { return item.userId === booking.teacherId; });
      var title = teacher ? teacher.name : text("teacherFallback");
      return '<article class="booking-row"><div><strong>' + escapeHtml(title) + '</strong><p class="muted">' + when(booking.startTime) + ' - ' + when(booking.endTime) + '</p><span class="badge">' + translateStatus(booking.status) + '</span></div><div class="booking-actions">' + actionButtons(booking) + '</div></article>';
    }).join("");
  }

  function actionButtons(booking) {
    var canConfirm = state.user && (state.user.role === 1 || state.user.role === "Teacher") && booking.status === "Pending";
    var buttons = [];
    if (canConfirm) buttons.push('<button data-action="confirm" data-booking="' + booking.id + '">' + text("confirm") + '</button>');
    if (canConfirm) buttons.push('<button class="secondary" data-action="reject" data-booking="' + booking.id + '">' + text("reject") + '</button>');
    if (booking.status === "Confirmed") buttons.push('<button data-action="pay" data-booking="' + booking.id + '">' + text("pay") + '</button>');
    if (booking.status === "Paid") buttons.push('<button data-join="' + booking.id + '">' + text("joinCall") + '</button>');
    if (booking.status === "Paid") buttons.push('<button class="secondary" data-action="complete" data-booking="' + booking.id + '">' + text("complete") + '</button>');
    return buttons.join("");
  }

  function bookingAction(id, action) {
    var body = action === "confirm" || action === "reject" ? { teacherId: state.user.id } : { studentId: state.user.id };
    post("/api/bookings/" + id + "/" + action, body).then(function () {
      loadBookings();
    }).catch(function (err) { toast(err.message); });
  }

  function selectTeacher(id) {
    state.selectedTeacher = state.teachers.find(function (teacher) { return teacher.id === id; });
    if (!state.selectedTeacher) return;
    el("chatTitle").textContent = state.selectedTeacher.name;
    el("chatStatus").textContent = state.selectedTeacher.isOnline ? text("online") : text("offline");
    loadMessages();
    location.hash = "chat";
  }

  function loadMessages() {
    if (!state.user || !state.selectedTeacher) return;
    json("/api/messages?userA=" + state.user.id + "&userB=" + state.selectedTeacher.userId).then(function (messages) {
      el("messages").innerHTML = messages.map(function (message) {
        var mine = message.senderId === state.user.id ? " mine" : "";
        return '<div class="bubble' + mine + '">' + escapeHtml(message.content) + '<time>' + when(message.createdAt) + '</time></div>';
      }).join("");
      el("messages").scrollTop = el("messages").scrollHeight;
    });
  }

  function sendMessage(event) {
    event.preventDefault();
    if (!state.selectedTeacher) return toast(text("selectTeacherFirst"));
    var content = el("messageInput").value.trim();
    if (!content) return;
    el("messageInput").value = "";
    post("/api/messages", { senderId: state.user.id, receiverId: state.selectedTeacher.userId, content: content })
      .then(loadMessages)
      .catch(function (err) { toast(err.message); });
  }

  function joinCall(bookingId) {
    json("/api/calls/" + bookingId + "/access?userId=" + state.user.id).then(function (access) {
      el("callState").textContent = access.canJoin ? text("connectedCall") : text("paidBookingRequired");
      location.hash = "call";
    });
  }

  function renderStats() {
    var paid = state.bookings.filter(function (booking) { return booking.status === "Paid"; }).length;
    el("statBookings").textContent = state.bookings.length;
    el("statPaid").textContent = paid;
    el("statOnline").textContent = state.teachers.filter(function (teacher) { return teacher.isOnline; }).length;
    el("statHeld").textContent = money(paid * 200000);
  }

  function translateStatus(status) {
    return text("statuses")[status] || status;
  }

  function bindEvents() {
    el("language").addEventListener("change", function (event) { setLanguage(event.target.value); });
    el("loginBtn").addEventListener("click", login);
    el("search").addEventListener("input", loadTeachers);
    el("mode").addEventListener("change", loadTeachers);
    el("refreshBookings").addEventListener("click", loadBookings);
    el("messageForm").addEventListener("submit", sendMessage);
    el("cameraBtn").addEventListener("click", function () { toast(text("cameraToggled")); });
    el("micBtn").addEventListener("click", function () { toast(text("micToggled")); });
    el("endCallBtn").addEventListener("click", function () { el("callState").textContent = text("callEnded"); });

    document.body.addEventListener("click", function (event) {
      var target = event.target;
      if (target.getAttribute("data-book")) bookTeacher(target.getAttribute("data-book"));
      if (target.getAttribute("data-chat")) selectTeacher(target.getAttribute("data-chat"));
      if (target.getAttribute("data-action")) bookingAction(target.getAttribute("data-booking"), target.getAttribute("data-action"));
      if (target.getAttribute("data-join")) joinCall(target.getAttribute("data-join"));
    });
  }

  function restoreSession() {
    var cached = localStorage.getItem("skillbridge-user");
    if (cached) {
      try {
        state.user = JSON.parse(cached);
        renderUser();
      } catch (error) {
        localStorage.removeItem("skillbridge-user");
      }
    }
    loadTeachers();
    if (state.user) loadBookings();
  }

  function init() {
    bindEvents();
    applyLanguage();
    restoreSession();
    window.skillBridgeAppLoaded = true;
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
}());
