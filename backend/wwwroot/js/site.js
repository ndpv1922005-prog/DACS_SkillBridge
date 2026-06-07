(function () {
  var selectedContactId = null;
  var selectedContactName = "";
  var currentUserId = window.skillBridgeCurrentUserId;
  var chatConnection = null;
  var i18n = window.skillBridgeText || {};

  function escapeHtml(value) {
    return String(value || "").replace(/[&<>"']/g, function (char) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" }[char];
    });
  }

  function normalizeMessage(message) {
    return {
      id: message.id || message.Id,
      senderId: String(message.senderId || message.SenderId),
      receiverId: String(message.receiverId || message.ReceiverId),
      content: message.content || message.Content || "",
      createdAt: message.createdAt || message.CreatedAt,
      isDeleted: Boolean(message.isDeleted || message.IsDeleted)
    };
  }

  function formatTime(value) {
    if (!value) return "";
    return new Date(value).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
  }

  function text(key, fallback) {
    return i18n[key] || fallback;
  }

  function getJson(url) {
    return fetch(url).then(function (response) {
      if (!response.ok) throw new Error("Request failed");
      return response.json();
    });
  }

  function postJson(url, body) {
    return fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    }).then(function (response) {
      if (!response.ok) throw new Error("Request failed");
      return response.json();
    });
  }

  function refreshChatNavBadge() {
    return Promise.resolve();
  }

  function renderMessage(message) {
    var item = normalizeMessage(message);
    var mine = item.senderId === String(currentUserId);
    var deletedClass = item.isDeleted ? " is-deleted" : "";

    return [
      '<div class="message-row' + (mine ? " mine" : "") + '" data-message-id="' + item.id + '">',
      '  <div class="message-bubble' + deletedClass + '">',
      '    <div class="message-text">' + escapeHtml(item.content) + "</div>",
      '    <div class="message-meta"><span>' + formatTime(item.createdAt) + "</span></div>",
      "  </div>",
      "</div>"
    ].join("");
  }

  function clearMessages(text) {
    var container = document.getElementById("chatMessages");
    if (container) container.innerHTML = '<div class="chat-empty">' + escapeHtml(text) + "</div>";
  }

  function loadMessages() {
    if (!currentUserId || !selectedContactId) {
      clearMessages(text("chooseConversation", "Chọn một cuộc trò chuyện để bắt đầu"));
      return;
    }

    getJson("/api/messages?userA=" + encodeURIComponent(currentUserId) + "&userB=" + encodeURIComponent(selectedContactId))
      .then(function (messages) {
        var container = document.getElementById("chatMessages");
        if (!container) return;
        var history = Array.isArray(messages) ? messages : [];
        container.innerHTML = history.length
          ? history.map(renderMessage).join("")
          : '<div class="chat-empty">' + escapeHtml(text("noMessagesYet", "Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện.")) + "</div>";
        container.scrollTop = container.scrollHeight;
        clearContactUnread(selectedContactId);
        return postJson("/api/messages/read", { userId: currentUserId, contactUserId: selectedContactId });
      })
      .then(function (result) {
        if (result) setChatNavBadge(Number(result.unreadChat || result.UnreadChat || 0));
        return refreshChatNavBadge();
      })
      .catch(function () {
        clearMessages(text("noMessagesYet", "Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện."));
      });
  }

  function appendMessage(message) {
    var item = normalizeMessage(message);
    if (!selectedContactId || (item.senderId !== String(selectedContactId) && item.receiverId !== String(selectedContactId))) {
      updateContactPreview(item);
      if (item.receiverId === String(currentUserId)) {
        markContactUnread(item.senderId);
        incrementChatNavBadge();
      }
      return;
    }

    var container = document.getElementById("chatMessages");
    if (!container) return;
    if (container.querySelector('[data-message-id="' + item.id + '"]')) return;
    if (container.querySelector(".chat-empty")) container.innerHTML = "";
    container.insertAdjacentHTML("beforeend", renderMessage(item));
    container.scrollTop = container.scrollHeight;
    updateContactPreview(item);
    if (item.receiverId === String(currentUserId)) {
      clearContactUnread(item.senderId);
      postJson("/api/messages/read", { userId: currentUserId, contactUserId: item.senderId })
        .then(function (result) {
          setChatNavBadge(Number(result.unreadChat || result.UnreadChat || 0));
          return refreshChatNavBadge();
        })
        .catch(function () {});
    }
  }

  function replaceDeletedMessage(message) {
    var item = normalizeMessage(message);
    var existing = document.querySelector('[data-message-id="' + item.id + '"]');
    if (existing) {
      existing.outerHTML = renderMessage(item);
    }
    updateContactPreview(item);
  }

  function updateContactPreview(message) {
    var item = normalizeMessage(message);
    var contactId = item.senderId === String(currentUserId) ? item.receiverId : item.senderId;
    var button = document.querySelector('.chat-contact[data-user-id="' + contactId + '"]');
    if (!button) {
      if (item.receiverId === String(currentUserId)) {
        window.location.reload();
      }
      return;
    }
    var last = button.querySelector(".chat-contact-last");
    var time = button.querySelector(".chat-contact-top small");
    if (last) last.textContent = item.content;
    if (time) time.textContent = formatTime(item.createdAt);
    button.parentElement?.prepend(button);
  }

  function incrementBadge(element) {
    if (!element) return;
    var current = parseInt(element.textContent || "0", 10);
    var next = Number.isFinite(current) ? current + 1 : 1;
    element.textContent = next > 9 ? "9+" : String(next);
  }

  function incrementChatNavBadge() {
  }

  function setChatNavBadge(count) {
  }

  function markContactUnread(contactId) {
    var button = document.querySelector('.chat-contact[data-user-id="' + contactId + '"]');
    if (!button) return;
    button.classList.add("has-unread");
    var badge = button.querySelector(".chat-contact-unread");
    if (!badge) {
      badge = document.createElement("span");
      badge.className = "chat-contact-unread";
      var top = button.querySelector(".chat-contact-top");
      top?.appendChild(badge);
    }
    incrementBadge(badge);
  }

  function clearContactUnread(contactId) {
    var button = document.querySelector('.chat-contact[data-user-id="' + contactId + '"]');
    if (!button) return;
    button.classList.remove("has-unread");
    button.querySelector(".chat-contact-unread")?.remove();
  }

  function startChatConnection() {
    if (!currentUserId || !window.signalR) return;

    chatConnection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/skillbridge")
      .withAutomaticReconnect()
      .build();

    chatConnection.on("MessageReceived", appendMessage);
    chatConnection.on("MessageDeleted", replaceDeletedMessage);
    chatConnection.start()
      .then(function () {
        return chatConnection.invoke("JoinUserGroup", currentUserId);
      })
      .catch(function () {
        chatConnection = null;
      });
  }

  document.addEventListener("click", function (event) {
    var contact = event.target.closest(".teacher-chat");
    if (contact) {
      selectedContactId = contact.getAttribute("data-user-id");
      selectedContactName = contact.getAttribute("data-name") || "";
      document.querySelectorAll(".chat-contact").forEach(function (item) {
        item.classList.toggle("active", item === contact);
      });

      var title = document.getElementById("chatTitle");
      var subtitle = document.getElementById("chatSubtitle");
      var avatar = document.getElementById("chatAvatar");
      if (title) title.textContent = selectedContactName;
      if (subtitle) subtitle.textContent = contact.getAttribute("data-role") || "";
      if (avatar) {
        avatar.src = contact.getAttribute("data-avatar") || "";
        avatar.classList.remove("d-none");
      }
      clearContactUnread(selectedContactId);
      postJson("/api/messages/read", { userId: currentUserId, contactUserId: selectedContactId })
        .then(function (result) {
          setChatNavBadge(Number(result.unreadChat || result.UnreadChat || 0));
          return refreshChatNavBadge();
        })
        .catch(function () {});
      var deleteConversationButton = document.getElementById("deleteConversationButton");
      if (deleteConversationButton) deleteConversationButton.classList.remove("d-none");
      loadMessages();
      return;
    }

    var deleteConversationButton = event.target.closest("#deleteConversationButton");
    if (deleteConversationButton) {
      if (!selectedContactId || !currentUserId) return;
      var confirmed = window.confirm(text("confirmDeleteConversation", "Bạn có chắc muốn xóa cuộc trò chuyện này khỏi lịch sử của bạn không?"));
      if (!confirmed) return;

      postJson("/api/conversations/hide", { userId: currentUserId, contactUserId: selectedContactId })
        .then(function () {
          var contactButton = document.querySelector('.chat-contact[data-user-id="' + selectedContactId + '"]');
          if (contactButton) {
            var last = contactButton.querySelector(".chat-contact-last");
            var time = contactButton.querySelector(".chat-contact-top small");
            if (last) last.textContent = text("noMessages", "Chưa có tin nhắn");
            if (time) time.textContent = "";
          }
          clearMessages(text("noMessagesYet", "Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện."));
        });
    }
  });

  document.addEventListener("show.bs.modal", function (event) {
    var modal = event.target;
    if (!modal.classList.contains("fade") || (!modal.querySelector(".complaint-modal") && !modal.querySelector(".registered-teacher-modal"))) return;
    if (modal.querySelector(".registered-teacher-modal")) {
      document.body.classList.add("registered-teacher-modal-open");
    }
    if (modal.parentElement !== document.body) {
      document.body.appendChild(modal);
    }
  });

  document.addEventListener("hidden.bs.modal", function (event) {
    if (!event.target.querySelector(".complaint-modal") && !event.target.querySelector(".registered-teacher-modal")) return;
    if (event.target.querySelector(".registered-teacher-modal")) {
      document.body.classList.remove("registered-teacher-modal-open");
    }
    document.querySelectorAll(".modal-backdrop").forEach(function (backdrop) {
      backdrop.remove();
    });
    document.body.classList.remove("modal-open");
    document.body.style.removeProperty("overflow");
    document.body.style.removeProperty("padding-right");
  });

  document.addEventListener("submit", function (event) {
    if (event.target.id !== "chatForm") return;
    event.preventDefault();

    var input = document.getElementById("messageInputMvc");
    if (!selectedContactId) {
      clearMessages(text("chooseConversation", "Chọn một cuộc trò chuyện để bắt đầu"));
      return;
    }
    if (!input || !input.value.trim() || !currentUserId) return;

    var payload = {
      senderId: currentUserId,
      receiverId: selectedContactId,
      content: input.value.trim()
    };

    input.value = "";
    if (chatConnection && chatConnection.state === signalR.HubConnectionState.Connected) {
      chatConnection.invoke("SendMessage", payload).catch(function () {
        postJson("/api/messages", payload).then(appendMessage);
      });
      return;
    }

    postJson("/api/messages", payload).then(appendMessage);
  });

  document.addEventListener("keydown", function (event) {
    var input = event.target.closest("#messageInputMvc");
    if (!input || event.key !== "Enter" || event.shiftKey) return;
    event.preventDefault();
    var form = document.getElementById("chatForm");
    if (form?.requestSubmit) {
      form.requestSubmit();
    } else {
      form?.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
    }
  });

  document.addEventListener("change", function (event) {
    var complaintReason = event.target.closest("[data-complaint-reason]");
    if (complaintReason) {
      var modal = complaintReason.closest(".modal-content");
      var other = modal?.querySelector("[data-complaint-other]");
      if (other) {
        var isOther = complaintReason.value === "Khác" || complaintReason.value === "Other" || complaintReason.selectedIndex === complaintReason.options.length - 1;
        other.classList.toggle("d-none", !isOther);
        other.required = isOther;
        if (!isOther) other.value = "";
      }
    }

    var input = event.target.closest("[data-student-avatar-input]");
    if (!input || !input.files || !input.files[0]) return;
    var preview = document.querySelector("[data-student-avatar-preview]");
    if (!preview) return;
    preview.src = URL.createObjectURL(input.files[0]);
  });

  function setPasswordVisibility(button, visible) {
    var group = button.closest(".password-hold-group");
    if (!group) return;
    var input = group.querySelector("[data-password-field]");
    if (!input) return;
    input.type = visible ? "text" : "password";
    button.classList.toggle("active", visible);
  }

  document.addEventListener("pointerdown", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (!button) return;
    event.preventDefault();
    button.setPointerCapture?.(event.pointerId);
    setPasswordVisibility(button, true);
  });

  document.addEventListener("pointerup", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (button) setPasswordVisibility(button, false);
  });

  document.addEventListener("pointercancel", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (button) setPasswordVisibility(button, false);
  });

  document.addEventListener("pointerleave", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (button) setPasswordVisibility(button, false);
  });

  document.addEventListener("keydown", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (!button || (event.key !== " " && event.key !== "Enter")) return;
    event.preventDefault();
    setPasswordVisibility(button, true);
  });

  document.addEventListener("keyup", function (event) {
    var button = event.target.closest("[data-password-hold]");
    if (!button || (event.key !== " " && event.key !== "Enter")) return;
    setPasswordVisibility(button, false);
  });

  var initialContactId = window.skillBridgeInitialContactId;
  if (initialContactId) {
    document.querySelector('.chat-contact[data-user-id="' + initialContactId + '"]')?.click();
  }

  if (document.querySelector(".chat-shell")) {
    refreshChatNavBadge();
  }

  startChatConnection();
}());
