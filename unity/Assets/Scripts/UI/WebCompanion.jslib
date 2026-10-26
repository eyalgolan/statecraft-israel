// Outbound only. Bootstrap travels with the hashed player: cached stock HTML also receives the companion.
// An existing custom callback is respected. Pages without the stock canvas keep their own layout.
mergeInto(LibraryManager.library, {
  StatecraftCompanion: function (json) {
    if (typeof window === "undefined") return;
    if (typeof window.statecraftCompanion !== "function") {
      if (!document.getElementById("unity-canvas")) return;
      var style = document.createElement("style");
      style.id = "companion-style";
      style.textContent =
        "      body.has-companion #unity-canvas { width: calc(100% - 22rem); }\n" +
        "      #companion { position: fixed; right: 0; top: 0; bottom: 0; box-sizing: border-box;\n" +
        "        width: 22rem; overflow: auto; padding: 1rem; color: #ece4d2; background: #0e1626;\n" +
        "        border-left: 1px solid #c4a05c; font: 16px/1.5 Georgia, serif; overflow-wrap: anywhere; }\n" +
        "      #companion p, #companion li { white-space: pre-wrap; }\n" +
        "      #companion h2, #companion h3 { font-size: 1.15rem; }\n" +
        "      #companion ol { padding-inline: 0; list-style-position: inside; }\n" +
        "      @media (max-width: 900px) {\n" +
        "        body.has-companion #unity-canvas { width: 100%; height: 65%; }\n" +
        "        #companion { top: 65%; width: 100%; border-left: 0; border-top: 1px solid #c4a05c; }\n" +
        "      }";
      document.head.appendChild(style);
      var nodes = {};
      [
        [
          "aside",
          "companion",
          null,
          {
            "aria-labelledby": "companion-heading",
            "hidden": ""
          }
        ],
        [
          "h2",
          "companion-heading",
          "companion",
          {}
        ],
        [
          "p",
          "companion-help",
          "companion",
          {}
        ],
        [
          "section",
          "companion-event",
          "companion",
          {
            "aria-live": "polite",
            "aria-atomic": "true"
          }
        ],
        [
          "h3",
          "companion-title",
          "companion-event",
          {}
        ],
        [
          "div",
          "companion-body",
          "companion-event",
          {}
        ],
        [
          "div",
          "companion-choice-group",
          "companion-event",
          {}
        ],
        [
          "h3",
          "companion-choice-heading",
          "companion-choice-group",
          {}
        ],
        [
          "ol",
          "companion-choices",
          "companion-choice-group",
          {}
        ],
        [
          "h3",
          "companion-focus-heading",
          "companion",
          {}
        ],
        [
          "p",
          "companion-focus",
          "companion",
          {
            "aria-live": "polite",
            "aria-atomic": "true"
          }
        ],
        [
          "h3",
          "companion-status-heading",
          "companion",
          {}
        ],
        [
          "p",
          "companion-status",
          "companion",
          {
            "role": "status",
            "aria-atomic": "true"
          }
        ]
      ].forEach(function (spec) {
        var e = document.createElement(spec[0]); e.id = spec[1]; e.dir = "auto";
        Object.keys(spec[3]).forEach(function (key) { e.setAttribute(key, spec[3][key]); });
        (spec[2] ? nodes[spec[2]] : document.body).appendChild(e);
        nodes[spec[1]] = e;
      });
      var companionPrevious = null;
      window.statecraftCompanion = function(json) {
        var snapshot = JSON.parse(json);
        var panel = document.getElementById("companion");
        if (!panel) return; // A custom callback may own a different DOM.
        var node = function (part) { return document.getElementById("companion-" + part); };
        var words = function (part, value) {
          var element = node(part);
          if (element.textContent !== value) element.textContent = value;
        };
        document.body.classList.toggle("has-companion", snapshot !== null);
        if (snapshot === null) {
          panel.hidden = true;
          ["heading", "help", "title", "body", "choice-heading", "choices", "focus-heading", "focus", "status-heading", "status"]
            .forEach(function (part) { node(part).textContent = ""; });
          companionPrevious = null;
          return;
        }
        if (panel.lang !== snapshot.lang) panel.lang = snapshot.lang;
        // Containers need the UI direction; leaf paragraphs/choices still resolve mixed-script text independently.
        var direction = snapshot.lang === "he" ? "rtl" : "ltr";
        if (panel.dir !== direction) panel.dir = direction;
        // Each paragraph/choice has auto direction: Hebrew UI may have individual English fallback fields.
        words("heading", snapshot.heading);
        words("help", snapshot.help);
        words("title", snapshot.title || snapshot.empty);
        words("choice-heading", snapshot.choicesHeading);
        words("focus-heading", snapshot.focusHeading);
        words("focus", snapshot.focus);
        words("status-heading", snapshot.statusHeading);
        words("status", snapshot.status);
        var previous = companionPrevious;
        if (!previous || JSON.stringify(previous.body) !== JSON.stringify(snapshot.body)) {
          node("body").textContent = "";
          snapshot.body.forEach(function (line) {
            var p = document.createElement("p"); p.dir = "auto"; p.textContent = line;
            node("body").appendChild(p);
          });
        }
        node("choice-group").hidden = snapshot.choices.length === 0;
        if (!previous || previous.unavailable !== snapshot.unavailable || JSON.stringify(previous.choices) !== JSON.stringify(snapshot.choices)) {
          node("choices").textContent = "";
          snapshot.choices.forEach(function (choice) {
            var li = document.createElement("li"); li.dir = "auto";
            li.textContent = choice.text + (choice.available ? "" : " — " + snapshot.unavailable)
              + (choice.note ? "\n" + choice.note : "");
            node("choices").appendChild(li);
          });
        }
        companionPrevious = snapshot;
        panel.hidden = false;
      };
    }
    window.statecraftCompanion(UTF8ToString(json));
  }
});
