/*!
 * Cities: Skylines II UI Module
 *
 * Id: GPTStuntYourGrowth
 * Author: X_Bean_X
 * Version: 1.75.639
 * Dependencies:
 */
const { createElement } = window.React;
const { Button, Tooltip } = window["cs2/ui"];
const { trigger } = window["cs2/api"];
const title = "GPT's Puppet Master";

function GrowthToolbarButton() {
    return createElement(Tooltip, { tooltip: title },
        createElement(Button, {
            variant: "floating",
            style: { padding: 0, backgroundColor: "transparent", overflow: "hidden" },
            "aria-label": title,
            tooltipLabel: title,
            onSelect: () => trigger("GPTStuntYourGrowth", "openSettings")
        }, createElement("img", {
            src: "coui://ui-mods/GPTPuppetMasterRiverfront.png",
            alt: "",
            draggable: false,
            style: { width: "100%", height: "100%", display: "block", objectFit: "contain", pointerEvents: "none" }
        })));
}

export const hasCSS = false;

export default function register(registry) {
    registry.append("GameTopLeft", GrowthToolbarButton);
}
