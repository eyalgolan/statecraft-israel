// Outbound only, and optional: nothing here may throw back into the Unity call that published a snapshot. A malformed
// snapshot, a missing element or a failing page script leaves the last good panel, and a failed bootstrap is tried again
// on the next snapshot. The stock page carries the panel from load, so its one live region (the status line) exists
// before the first announcement; a cached older page gets the same elements from the bootstrap below, which travels with
// the hashed player. An existing custom callback is respected. Pages without the stock canvas keep their own layout.
mergeInto(LibraryManager.library, {
  StatecraftCompanion: function (json) {
    try {
      var payload = UTF8ToString(json);
      if (typeof window === "undefined" || typeof document === "undefined" || !document) return;
      if (typeof window.statecraftCompanion !== "function") {
        if (!document.head || !document.body || typeof document.createElement !== "function" ||
            typeof document.getElementById !== "function" || !document.getElementById("unity-canvas")) return;
        if (!document.getElementById("companion-style")) {
          var style = document.createElement("style");
          style.id = "companion-style";
          style.textContent =
            "      body:not(.has-companion) #companion { position: absolute; width: 1px; height: 1px; overflow: hidden;\n" +
            "        clip-path: inset(50%); white-space: nowrap; }\n" +
            "      body.has-companion #unity-canvas { width: calc(100% - 22rem); }\n" +
            "      body.has-companion #companion { position: fixed; right: 0; top: 0; bottom: 0; box-sizing: border-box;\n" +
            "        width: 22rem; overflow: auto; color: #ece4d2; background: #0e1626;\n" +
            "        padding: max(1rem, env(safe-area-inset-top)) max(1rem, env(safe-area-inset-right))\n" +
            "          max(1rem, env(safe-area-inset-bottom)) 1rem;\n" +
            "        border-left: 1px solid #c4a05c; font: 16px/1.5 Georgia, serif; overflow-wrap: anywhere; }\n" +
            "      #companion p, #companion li { white-space: pre-wrap; }\n" +
            "      #companion h2, #companion h3 { font-size: 1.15rem; }\n" +
            "      #companion ol { padding-inline: 0; list-style-position: inside; }\n" +
            "      @media (max-width: 900px) {\n" +
            "        body.has-companion #unity-canvas { width: 100%; height: 65%; }\n" +
            "        body.has-companion #companion { top: 65%; width: 100%; border-left: 0; border-top: 1px solid #c4a05c;\n" +
            "          padding: 1rem max(1rem, env(safe-area-inset-right)) max(1rem, env(safe-area-inset-bottom))\n" +
            "            max(1rem, env(safe-area-inset-left)); }\n" +
            "      }";
          document.head.appendChild(style);
        }
        // Tag, id, parent id, attributes: the same elements, in the same order, as the stock page's skeleton (index.html).
        var specs = [
          ["aside", "companion", null, { "aria-labelledby": "companion-heading", "tabindex": "-1", "dir": "auto" }],
          ["h2", "companion-heading", "companion", { "dir": "auto" }],
          ["p", "companion-help", "companion", { "dir": "auto" }],
          ["section", "companion-event", "companion", { "dir": "auto" }],
          ["h3", "companion-title", "companion-event", { "dir": "auto" }],
          ["div", "companion-body", "companion-event", { "dir": "auto" }],
          ["div", "companion-choice-group", "companion-event", { "dir": "auto" }],
          ["h3", "companion-choice-heading", "companion-choice-group", { "dir": "auto" }],
          ["ol", "companion-choices", "companion-choice-group", { "dir": "auto" }],
          ["h3", "companion-focus-heading", "companion", { "dir": "auto" }],
          ["p", "companion-focus", "companion", { "dir": "auto" }],
          ["h3", "companion-status-heading", "companion", { "dir": "auto" }],
          ["p", "companion-status", "companion", { "role": "status", "aria-atomic": "true", "dir": "auto" }],
          ["section", "companion-report", "companion", { "dir": "auto" }],
          ["h3", "companion-report-title", "companion-report", { "dir": "auto" }],
          ["div", "companion-report-rows", "companion-report", { "dir": "auto" }]
        ];
        for (var i = 0; i < specs.length; i++) {
          var spec = specs[i];
          if (document.getElementById(spec[1])) continue; // the stock page's own element, or a failed attempt's
          var parent = spec[2] ? document.getElementById(spec[2]) : document.body;
          if (!parent) return;
          var made = document.createElement(spec[0]);
          made.id = spec[1];
          for (var key in spec[3]) made.setAttribute(key, spec[3][key]);
          parent.appendChild(made);
        }
        var texts = ["heading", "help", "focusHeading", "statusHeading", "choicesHeading", "empty", "unavailable",
          "title", "focus", "status", "reportTitle"];
        var strings = function (list) {
          if (!Array.isArray(list)) return false;
          for (var n = 0; n < list.length; n++) if (typeof list[n] !== "string") return false;
          return true;
        };
        // The whole shape is checked before anything on the page changes.
        var valid = function (s) {
          if (s === null) return true;
          if (typeof s !== "object" || Array.isArray(s)) return false;
          if (s.lang !== "en" && s.lang !== "he") return false; // a fixed language, never game text
          if (typeof s.turn !== "number") return false;
          for (var n = 0; n < texts.length; n++) if (typeof s[texts[n]] !== "string") return false;
          if (!strings(s.body) || !strings(s.report) || !Array.isArray(s.choices)) return false;
          for (n = 0; n < s.choices.length; n++) {
            var c = s.choices[n];
            if (!c || typeof c.text !== "string" || typeof c.note !== "string" || typeof c.available !== "boolean") return false;
          }
          return true;
        };
        var parts = ["heading", "help", "title", "body", "choice-group", "choice-heading", "choices", "focus-heading",
          "focus", "status-heading", "status", "report", "report-title", "report-rows"];
        var cleared = ["heading", "help", "title", "body", "choice-heading", "choices", "focus-heading", "focus",
          "status-heading", "status", "report-title", "report-rows"];
        var previous = null;
        window.statecraftCompanion = function (json) {
          var snapshot;
          try { snapshot = JSON.parse(json); } catch (e) { return; }
          if (!valid(snapshot)) return;
          var panel = document.getElementById("companion");
          if (!panel) return; // A custom callback may own a different DOM.
          var node = {};
          for (var n = 0; n < parts.length; n++) {
            node[parts[n]] = document.getElementById("companion-" + parts[n]);
            if (!node[parts[n]]) return;
          }
          var words = function (part, value) { if (node[part].textContent !== value) node[part].textContent = value; };
          var lines = function (part, tag, values) {
            node[part].textContent = "";
            values.forEach(function (value) {
              var e = document.createElement(tag); e.setAttribute("dir", "auto"); e.textContent = value;
              node[part].appendChild(e);
            });
          };
          document.body.classList.toggle("has-companion", snapshot !== null);
          if (snapshot === null) {
            cleared.forEach(function (part) { node[part].textContent = ""; });
            node.report.hidden = true;
            previous = null;
            return;
          }
          if (panel.lang !== snapshot.lang) panel.lang = snapshot.lang;
          // Containers need the UI direction; leaf paragraphs/choices still resolve mixed-script text independently.
          var direction = snapshot.lang === "he" ? "rtl" : "ltr";
          if (panel.dir !== direction) panel.dir = direction;
          words("heading", snapshot.heading);
          words("help", snapshot.help);
          words("title", snapshot.title || snapshot.empty);
          words("choice-heading", snapshot.choicesHeading);
          words("focus-heading", snapshot.focusHeading);
          words("focus", snapshot.focus);
          words("status-heading", snapshot.statusHeading);
          // The one live region: written when its text changes, and written again when the same result comes on a new turn.
          if (node.status.textContent !== snapshot.status) node.status.textContent = snapshot.status;
          else if (previous && previous.turn !== snapshot.turn && snapshot.status !== "") {
            node.status.textContent = "";
            node.status.textContent = snapshot.status;
          }
          if (!previous || JSON.stringify(previous.body) !== JSON.stringify(snapshot.body)) lines("body", "p", snapshot.body);
          node["choice-group"].hidden = snapshot.choices.length === 0;
          if (!previous || previous.unavailable !== snapshot.unavailable || JSON.stringify(previous.choices) !== JSON.stringify(snapshot.choices))
            lines("choices", "li", snapshot.choices.map(function (choice) {
              return choice.text + (choice.available ? "" : ": " + snapshot.unavailable) + (choice.note ? "\n" + choice.note : "");
            }));
          node.report.hidden = snapshot.reportTitle === "" && snapshot.report.length === 0;
          words("report-title", snapshot.reportTitle);
          if (!previous || JSON.stringify(previous.report) !== JSON.stringify(snapshot.report)) lines("report-rows", "p", snapshot.report);
          previous = snapshot;
        };
      }
      window.statecraftCompanion(payload);
    } catch (e) {
      // Swallowed on purpose: the game must keep running whatever the page does with the mirror.
    }
  }
});
